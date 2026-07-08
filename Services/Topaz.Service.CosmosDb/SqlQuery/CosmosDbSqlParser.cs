using System.Text.Json;
using System.Text.Json.Nodes;

namespace Topaz.Service.CosmosDb.SqlQuery;

internal enum TokenKind
{
    Select, From, Where, Group, Order, By, Asc, Desc, Offset, Limit,
    And, Or, Not, In, Between, Value,
    Count, Sum, Min, Max, Avg,
    IsNull, IsDefined, IsString, IsNumber, IsBool,
    As,
    Star, Dot, Comma, LParen, RParen,
    Eq, Neq, Lt, Lte, Gt, Gte,
    StringLit, NumberLit, BoolLit, NullLit,
    Identifier, Parameter,
    Eof
}

internal readonly struct Token(TokenKind kind, string text, object? value = null)
{
    internal TokenKind Kind { get; } = kind;
    internal string Text { get; } = text;
    internal object? LiteralValue { get; } = value;
}

internal sealed class ParsedQuery
{
    internal SelectClause Select { get; init; } = default!;
    internal string FromAlias { get; init; } = "c";
    internal SqlExpression? Where { get; init; }
    internal string? GroupByField { get; init; }
    internal string? OrderByPath { get; init; }
    internal bool OrderByAscending { get; init; } = true;
    internal int Offset { get; init; }
    internal int? Limit { get; init; }
}

internal sealed class SelectClause
{
    internal bool IsWildcard { get; init; }
    internal bool IsValue { get; init; }
    internal SelectItem[] Items { get; init; } = [];
}

internal abstract class SelectItem { }

internal sealed class PropertySelectItem : SelectItem
{
    internal string PropertyPath { get; init; } = string.Empty;
    internal string? Alias { get; init; }
}

internal sealed class AggregateSelectItem : SelectItem
{
    internal AggregateFunction Function { get; init; }
    internal string? PropertyPath { get; init; }
    internal string? Alias { get; init; }
}

internal enum AggregateFunction { Count, Sum, Min, Max, Avg }

internal abstract class SqlExpression
{
    internal abstract bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters);
}

internal sealed class AndExpression(SqlExpression left, SqlExpression right) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters) =>
        left.Evaluate(doc, parameters) && right.Evaluate(doc, parameters);
}

internal sealed class OrExpression(SqlExpression left, SqlExpression right) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters) =>
        left.Evaluate(doc, parameters) || right.Evaluate(doc, parameters);
}

internal sealed class NotExpression(SqlExpression operand) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters) =>
        !operand.Evaluate(doc, parameters);
}

internal enum ComparisonOp { Eq, Neq, Lt, Lte, Gt, Gte }

internal sealed class ComparisonExpression(string propertyPath, ComparisonOp op, SqlValue right) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters)
    {
        var left = CosmosDbSqlExecutor.GetProperty(doc, propertyPath);
        var rightVal = right.Resolve(parameters);
        return CosmosDbSqlExecutor.Compare(left, op, rightVal);
    }
}

internal sealed class InExpression(string propertyPath, SqlValue[] values) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters)
    {
        var left = CosmosDbSqlExecutor.GetProperty(doc, propertyPath);
        return values.Any(v => CosmosDbSqlExecutor.Compare(left, ComparisonOp.Eq, v.Resolve(parameters)));
    }
}

internal sealed class BetweenExpression(string propertyPath, SqlValue low, SqlValue high) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters)
    {
        var val = CosmosDbSqlExecutor.GetProperty(doc, propertyPath);
        return CosmosDbSqlExecutor.Compare(val, ComparisonOp.Gte, low.Resolve(parameters)) &&
               CosmosDbSqlExecutor.Compare(val, ComparisonOp.Lte, high.Resolve(parameters));
    }
}

internal enum IsTypeCheck { Null, Defined, String, Number, Bool }

internal sealed class IsTypeExpression(string propertyPath, IsTypeCheck check) : SqlExpression
{
    internal override bool Evaluate(JsonObject doc, IReadOnlyDictionary<string, JsonNode?> parameters)
    {
        var val = CosmosDbSqlExecutor.GetProperty(doc, propertyPath);
        return check switch
        {
            IsTypeCheck.Null    => val is JsonValue jv && jv.GetValueKind() == JsonValueKind.Null,
            IsTypeCheck.Defined => val != null,
            IsTypeCheck.String  => val is JsonValue sv && sv.GetValueKind() == JsonValueKind.String,
            IsTypeCheck.Number  => val is JsonValue nv && nv.GetValueKind() == JsonValueKind.Number,
            IsTypeCheck.Bool    => val is JsonValue bv &&
                                   (bv.GetValueKind() == JsonValueKind.True ||
                                    bv.GetValueKind() == JsonValueKind.False),
            _ => false
        };
    }
}

internal abstract class SqlValue
{
    internal abstract object? Resolve(IReadOnlyDictionary<string, JsonNode?> parameters);
}

internal sealed class LiteralSqlValue(object? value) : SqlValue
{
    internal override object? Resolve(IReadOnlyDictionary<string, JsonNode?> parameters) => value;
}

internal sealed class ParameterSqlValue(string name) : SqlValue
{
    internal override object? Resolve(IReadOnlyDictionary<string, JsonNode?> parameters)
    {
        if (!parameters.TryGetValue(name, out var node)) return null;
        if (node == null) return null;
        if (node is not JsonValue jv) return node.ToString();
        if (jv.TryGetValue<double>(out var d)) return d;
        if (jv.TryGetValue<string>(out var s)) return s;
        if (jv.TryGetValue<bool>(out var b)) return b;

        return node.ToString();
    }
}

// ─── Parser ───────────────────────────────────────────────────────────────────

/// <summary>
/// Hand-rolled tokenizer and recursive-descent parser for the Cosmos DB SQL subset
/// required by the .NET SDK and Azure CLI:
/// <c>SELECT</c>, <c>FROM</c>, <c>WHERE</c>, <c>ORDER BY</c>, <c>OFFSET/LIMIT</c>,
/// <c>COUNT/SUM/MIN/MAX/AVG</c>, <c>IN</c>, <c>BETWEEN</c>, <c>IS_NULL</c>,
/// <c>IS_DEFINED</c>, <c>IS_STRING</c>, <c>IS_NUMBER</c>, <c>IS_BOOL</c>,
/// and parameterised queries (<c>@name</c>).
/// </summary>
internal sealed class CosmosDbSqlParser
{
    private List<Token> _tokens = [];
    private int _pos;
    private string _fromAlias = "c";

    internal ParsedQuery Parse(string sql)
    {
        _tokens = Tokenize(sql);
        _pos = 0;

        Expect(TokenKind.Select);
        var select = ParseSelectClause();

        Expect(TokenKind.From);
        _fromAlias = ExpectIdentifier();

        SqlExpression? where = null;
        if (CurrentIs(TokenKind.Where))
        {
            Advance();
            where = ParseOr();
        }

        string? groupByField = null;
        if (CurrentIs(TokenKind.Group))
        {
            Advance();
            Expect(TokenKind.By);
            groupByField = ParsePropertyPath();
        }

        string? orderByPath = null;
        var orderByAsc = true;
        if (CurrentIs(TokenKind.Order))
        {
            Advance();
            Expect(TokenKind.By);
            orderByPath = ParsePropertyPath();
            if (CurrentIs(TokenKind.Desc)) { Advance(); orderByAsc = false; }
            else if (CurrentIs(TokenKind.Asc)) Advance();
        }

        var offset = 0;
        int? limit = null;
        if (CurrentIs(TokenKind.Offset))
        {
            Advance();
            offset = (int)(double)ExpectNumber();
            Expect(TokenKind.Limit);
            limit = (int)(double)ExpectNumber();
        }
        else if (CurrentIs(TokenKind.Limit))
        {
            Advance();
            limit = (int)(double)ExpectNumber();
        }

        return new ParsedQuery
        {
            Select = select,
            FromAlias = _fromAlias,
            Where = where,
            GroupByField = groupByField,
            OrderByPath = orderByPath,
            OrderByAscending = orderByAsc,
            Offset = offset,
            Limit = limit
        };
    }

    private SelectClause ParseSelectClause()
    {
        // SELECT *
        if (CurrentIs(TokenKind.Star))
        {
            Advance();
            return new SelectClause { IsWildcard = true };
        }

        // SELECT VALUE ...
        if (CurrentIs(TokenKind.Value))
        {
            Advance();
            if (IsAggregateKeyword(Current().Kind))
            {
                var agg = ParseAggregateItem();
                return new SelectClause { IsValue = true, Items = [agg] };
            }

            var path = ParsePropertyPath();
            return new SelectClause
            {
                IsValue = true,
                Items = [new PropertySelectItem { PropertyPath = path }]
            };
        }

        // SELECT item [, item]*
        var items = new List<SelectItem> { ParseSelectItem() };
        while (CurrentIs(TokenKind.Comma))
        {
            Advance();
            items.Add(ParseSelectItem());
        }

        return new SelectClause { Items = [.. items] };
    }

    private SelectItem ParseSelectItem()
    {
        if (IsAggregateKeyword(Current().Kind))
            return ParseAggregateItem();

        var path = ParsePropertyPath();
        string? alias = null;
        if (CurrentIs(TokenKind.As))
        {
            Advance();
            alias = ExpectIdentifierOrKeyword();
        }

        return new PropertySelectItem { PropertyPath = path, Alias = alias };
    }

    private AggregateSelectItem ParseAggregateItem()
    {
        var fn = Current().Kind switch
        {
            TokenKind.Count => AggregateFunction.Count,
            TokenKind.Sum   => AggregateFunction.Sum,
            TokenKind.Min   => AggregateFunction.Min,
            TokenKind.Max   => AggregateFunction.Max,
            TokenKind.Avg   => AggregateFunction.Avg,
            _ => throw new InvalidOperationException($"Expected aggregate function, got '{Current().Text}'")
        };
        Advance();
        Expect(TokenKind.LParen);

        string? propertyPath = null;
        if (fn == AggregateFunction.Count && (CurrentIs(TokenKind.NumberLit) || CurrentIs(TokenKind.Star)))
        {
            Advance(); // consume '1' or '*'
        }
        else
        {
            propertyPath = ParsePropertyPath();
        }

        Expect(TokenKind.RParen);

        string? alias = null;
        if (CurrentIs(TokenKind.As))
        {
            Advance();
            alias = ExpectIdentifierOrKeyword();
        }

        return new AggregateSelectItem { Function = fn, PropertyPath = propertyPath, Alias = alias };
    }

    /// <summary>
    /// Parses a property path such as <c>c.address.city</c> and strips the
    /// collection alias (the first segment), returning <c>address.city</c>.
    /// </summary>
    private string ParsePropertyPath()
    {
        var parts = new List<string> { ExpectIdentifier() };
        while (CurrentIs(TokenKind.Dot))
        {
            Advance();
            parts.Add(ExpectIdentifierOrKeyword());
        }

        // Strip the leading alias segment (always present for valid Cosmos DB SQL like c.field).
        // A bare identifier (e.g. an aggregate alias like "cnt") has no dot, so return it as-is.
        return parts.Count > 1 ? string.Join(".", parts.Skip(1)) : parts[0];
    }

    private SqlExpression ParseOr()
    {
        var left = ParseAnd();
        while (CurrentIs(TokenKind.Or))
        {
            Advance();
            left = new OrExpression(left, ParseAnd());
        }

        return left;
    }

    private SqlExpression ParseAnd()
    {
        var left = ParseNot();
        while (CurrentIs(TokenKind.And))
        {
            Advance();
            left = new AndExpression(left, ParseNot());
        }

        return left;
    }

    private SqlExpression ParseNot()
    {
        if (!CurrentIs(TokenKind.Not)) return ParsePrimary();
        Advance();
        return new NotExpression(ParseNot());
    }

    private SqlExpression ParsePrimary()
    {
        // '(' condition ')'
        if (CurrentIs(TokenKind.LParen))
        {
            Advance();
            var inner = ParseOr();
            Expect(TokenKind.RParen);
            return inner;
        }

        // IS_* functions: IS_NULL(c.field), IS_DEFINED(c.field), …
        if (IsIsFunction(Current().Kind))
        {
            var check = Current().Kind switch
            {
                TokenKind.IsNull    => IsTypeCheck.Null,
                TokenKind.IsDefined => IsTypeCheck.Defined,
                TokenKind.IsString  => IsTypeCheck.String,
                TokenKind.IsNumber  => IsTypeCheck.Number,
                TokenKind.IsBool    => IsTypeCheck.Bool,
                _ => throw new InvalidOperationException()
            };
            Advance();
            Expect(TokenKind.LParen);
            var path = ParsePropertyPath();
            Expect(TokenKind.RParen);
            return new IsTypeExpression(path, check);
        }

        // All other predicates start with a property path
        var propPath = ParsePropertyPath();

        // IN
        if (CurrentIs(TokenKind.In))
        {
            Advance();
            Expect(TokenKind.LParen);
            var values = new List<SqlValue> { ParseValue() };
            while (CurrentIs(TokenKind.Comma))
            {
                Advance();
                values.Add(ParseValue());
            }

            Expect(TokenKind.RParen);
            return new InExpression(propPath, [.. values]);
        }

        // BETWEEN … AND …  (the AND is consumed here, not by ParseAnd)
        if (CurrentIs(TokenKind.Between))
        {
            Advance();
            var lo = ParseValue();
            Expect(TokenKind.And);
            var hi = ParseValue();
            return new BetweenExpression(propPath, lo, hi);
        }

        // Comparison
        var op = ParseCompOp();
        var rhs = ParseValue();
        return new ComparisonExpression(propPath, op, rhs);
    }

    private ComparisonOp ParseCompOp()
    {
        var kind = Current().Kind;
        Advance();
        return kind switch
        {
            TokenKind.Eq  => ComparisonOp.Eq,
            TokenKind.Neq => ComparisonOp.Neq,
            TokenKind.Lt  => ComparisonOp.Lt,
            TokenKind.Lte => ComparisonOp.Lte,
            TokenKind.Gt  => ComparisonOp.Gt,
            TokenKind.Gte => ComparisonOp.Gte,
            _ => throw new InvalidOperationException($"Expected comparison operator, got '{Current().Text}'")
        };
    }

    private SqlValue ParseValue()
    {
        var t = Current();
        Advance();
        return t.Kind switch
        {
            TokenKind.StringLit => new LiteralSqlValue(t.LiteralValue),
            TokenKind.NumberLit => new LiteralSqlValue(t.LiteralValue),
            TokenKind.BoolLit   => new LiteralSqlValue(t.LiteralValue),
            TokenKind.NullLit   => new LiteralSqlValue(null),
            TokenKind.Parameter => new ParameterSqlValue((string)t.LiteralValue!),
            _ => throw new InvalidOperationException($"Expected value, got '{t.Text}'")
        };
    }

    private static List<Token> Tokenize(string sql)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < sql.Length)
        {
            if (char.IsWhiteSpace(sql[i])) { i++; continue; }

            // Two-char operators
            if (i + 1 < sql.Length)
            {
                var two = sql.Substring(i, 2);
                switch (two)
                {
                    case "<=": tokens.Add(new Token(TokenKind.Lte, "<=")); i += 2; continue;
                    case ">=": tokens.Add(new Token(TokenKind.Gte, ">=")); i += 2; continue;
                    case "!=":
                    case "<>": tokens.Add(new Token(TokenKind.Neq, two)); i += 2; continue;
                }
            }

            // Single-char operators and symbols
            switch (sql[i])
            {
                case '=': tokens.Add(new Token(TokenKind.Eq,     "=")); i++; continue;
                case '<': tokens.Add(new Token(TokenKind.Lt,     "<")); i++; continue;
                case '>': tokens.Add(new Token(TokenKind.Gt,     ">")); i++; continue;
                case '*': tokens.Add(new Token(TokenKind.Star,   "*")); i++; continue;
                case '.': tokens.Add(new Token(TokenKind.Dot,    ".")); i++; continue;
                case ',': tokens.Add(new Token(TokenKind.Comma,  ",")); i++; continue;
                case '(': tokens.Add(new Token(TokenKind.LParen, "(")); i++; continue;
                case ')': tokens.Add(new Token(TokenKind.RParen, ")")); i++; continue;
            }

            // String literal (single-quoted)
            if (sql[i] == '\'')
            {
                i++;
                var start = i;
                while (i < sql.Length && sql[i] != '\'') i++;
                var s = sql[start..i];
                tokens.Add(new Token(TokenKind.StringLit, $"'{s}'", s));
                i++; // skip closing quote
                continue;
            }

            // Number literal
            if (char.IsDigit(sql[i]))
            {
                var start = i;
                while (i < sql.Length && (char.IsDigit(sql[i]) || sql[i] == '.')) i++;
                var num = sql[start..i];
                tokens.Add(new Token(TokenKind.NumberLit, num,
                    double.Parse(num, System.Globalization.CultureInfo.InvariantCulture)));
                continue;
            }

            // Parameter (@name)
            if (sql[i] == '@')
            {
                i++;
                var start = i;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_')) i++;
                var name = "@" + sql[start..i];
                tokens.Add(new Token(TokenKind.Parameter, name, name));
                continue;
            }

            // Keyword or identifier (IS_NULL etc. are tokenised here due to '_' support)
            if (char.IsLetter(sql[i]) || sql[i] == '_')
            {
                var start = i;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_')) i++;
                tokens.Add(ClassifyWord(sql[start..i]));
                continue;
            }

            i++; // skip unknown character
        }

        tokens.Add(new Token(TokenKind.Eof, ""));
        return tokens;
    }

    private static Token ClassifyWord(string word) =>
        word.ToUpperInvariant() switch
        {
            "SELECT"     => new Token(TokenKind.Select,    word),
            "FROM"       => new Token(TokenKind.From,      word),
            "WHERE"      => new Token(TokenKind.Where,     word),
            "GROUP"      => new Token(TokenKind.Group,     word),
            "ORDER"      => new Token(TokenKind.Order,     word),
            "BY"         => new Token(TokenKind.By,        word),
            "ASC"        => new Token(TokenKind.Asc,       word),
            "DESC"       => new Token(TokenKind.Desc,      word),
            "OFFSET"     => new Token(TokenKind.Offset,    word),
            "LIMIT"      => new Token(TokenKind.Limit,     word),
            "AND"        => new Token(TokenKind.And,       word),
            "OR"         => new Token(TokenKind.Or,        word),
            "NOT"        => new Token(TokenKind.Not,       word),
            "IN"         => new Token(TokenKind.In,        word),
            "BETWEEN"    => new Token(TokenKind.Between,   word),
            "VALUE"      => new Token(TokenKind.Value,     word),
            "COUNT"      => new Token(TokenKind.Count,     word),
            "SUM"        => new Token(TokenKind.Sum,       word),
            "MIN"        => new Token(TokenKind.Min,       word),
            "MAX"        => new Token(TokenKind.Max,       word),
            "AVG"        => new Token(TokenKind.Avg,       word),
            "AS"         => new Token(TokenKind.As,        word),
            "IS_NULL"    => new Token(TokenKind.IsNull,    word),
            "IS_DEFINED" => new Token(TokenKind.IsDefined, word),
            "IS_STRING"  => new Token(TokenKind.IsString,  word),
            "IS_NUMBER"  => new Token(TokenKind.IsNumber,  word),
            "IS_BOOL"    => new Token(TokenKind.IsBool,    word),
            "TRUE"       => new Token(TokenKind.BoolLit,   word, true),
            "FALSE"      => new Token(TokenKind.BoolLit,   word, false),
            "NULL"       => new Token(TokenKind.NullLit,   word),
            _            => new Token(TokenKind.Identifier, word)
        };

    private Token Current() => _tokens[_pos];
    private bool CurrentIs(TokenKind kind) => _tokens[_pos].Kind == kind;
    private void Advance() => _pos++;

    private void Expect(TokenKind kind)
    {
        if (!CurrentIs(kind))
            throw new InvalidOperationException(
                $"Expected {kind} but got '{Current().Text}' at token position {_pos}");
        Advance();
    }

    private string ExpectIdentifier()
    {
        var t = Current();
        if (t.Kind != TokenKind.Identifier)
            throw new InvalidOperationException(
                $"Expected identifier but got '{t.Text}' at token position {_pos}");
        Advance();
        return t.Text;
    }

    // Like ExpectIdentifier, but also accepts keyword tokens used as field names (e.g. c.value, c.avg)
    private string ExpectIdentifierOrKeyword()
    {
        var t = Current();
        if (t.Kind == TokenKind.Eof)
            throw new InvalidOperationException(
                $"Expected identifier but got EOF at token position {_pos}");
        Advance();
        return t.Text;
    }

    private object ExpectNumber()
    {
        var t = Current();
        if (t.Kind != TokenKind.NumberLit)
            throw new InvalidOperationException($"Expected number but got '{t.Text}'");
        Advance();
        return t.LiteralValue!;
    }

    private static bool IsAggregateKeyword(TokenKind k) =>
        k is TokenKind.Count or TokenKind.Sum or TokenKind.Min or TokenKind.Max or TokenKind.Avg;

    private static bool IsIsFunction(TokenKind k) =>
        k is TokenKind.IsNull or TokenKind.IsDefined or TokenKind.IsString
          or TokenKind.IsNumber or TokenKind.IsBool;
}
