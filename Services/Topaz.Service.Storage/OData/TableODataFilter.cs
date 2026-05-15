using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Topaz.Service.Storage.OData;

/// <summary>
/// Parses and evaluates Azure Table Storage OData v3 filter expressions against a <see cref="JsonObject"/>
/// entity.
///
/// Supported logical operators : and, or, not
/// Supported comparison operators: eq, ne, gt, ge, lt, le
/// Supported literal types:
///   string          — single-quoted, with '' escape for embedded quotes
///   int32           — plain integer, e.g. 42
///   int64           — integer with L/l suffix, e.g. 42L
///   bool            — true / false (case-insensitive)
///   datetime        — prefix form used by Azure Table Storage v3: datetime'yyyy-MM-ddTHH:mm:ssZ'
///   guid            — prefix form: guid'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
///   null            — null keyword
/// </summary>
internal static class TableODataFilter
{
    /// <summary>
    /// Evaluates <paramref name="filterExpression"/> against <paramref name="entity"/>.
    /// Returns <c>true</c> when the entity satisfies the filter, or when the filter expression
    /// cannot be parsed (fail-open so callers still receive data on unexpected predicates).
    /// </summary>
    public static bool Evaluate(string filterExpression, JsonObject entity)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            return true;

        try
        {
            var tokens = Tokenizer.Tokenize(filterExpression);
            var parser = new Parser(tokens);
            var expr = parser.ParseExpression();
            return Evaluator.Evaluate(expr, entity);
        }
        catch
        {
            return true; // fail-open on parse/eval errors
        }
    }

    // ═══════════════════════════════ AST ════════════════════════════════

    private abstract class ODataExpr;

    private sealed class LogicalExpr(ODataExpr left, bool isAnd, ODataExpr right) : ODataExpr
    {
        public ODataExpr Left { get; } = left;
        public bool IsAnd { get; } = isAnd;
        public ODataExpr Right { get; } = right;
    }

    private sealed class NotExpr(ODataExpr operand) : ODataExpr
    {
        public ODataExpr Operand { get; } = operand;
    }

    private sealed class CompareExpr(string property, CompOp op, ODataVal value) : ODataExpr
    {
        public string Property { get; } = property;
        public CompOp Op { get; } = op;
        public ODataVal Value { get; } = value;
    }

    private enum CompOp
    {
        Eq,
        Ne,
        Gt,
        Ge,
        Lt,
        Le
    }

    private abstract class ODataVal;

    private sealed class StrVal(string v) : ODataVal
    {
        public string V { get; } = v;
    }

    private sealed class I32Val(int v) : ODataVal
    {
        public int V { get; } = v;
    }

    private sealed class I64Val(long v) : ODataVal
    {
        public long V { get; } = v;
    }

    private sealed class BoolVal(bool v) : ODataVal
    {
        public bool V { get; } = v;
    }

    private sealed class DtVal(DateTimeOffset v) : ODataVal
    {
        public DateTimeOffset V { get; } = v;
    }

    private sealed class GuidVal(Guid v) : ODataVal
    {
        public Guid V { get; } = v;
    }

    private sealed class NullVal : ODataVal
    {
        public static readonly NullVal Instance = new();
    }

    // ═══════════════════════════════ TOKENS ═════════════════════════════

    private enum Tk
    {
        Ident,
        Str,
        Int,
        Long,
        Dt,
        Guid,
        LParen,
        RParen,
        Eof
    }

    private sealed class Tok(Tk kind, string text)
    {
        public Tk Kind { get; } = kind;
        public string Text { get; } = text;
    }

    // ═══════════════════════════════ TOKENIZER ═══════════════════════════

    private static class Tokenizer
    {
        public static List<Tok> Tokenize(string input)
        {
            var result = new List<Tok>();
            var i = 0;

            while (i < input.Length)
            {
                // Skip whitespace
                if (char.IsWhiteSpace(input[i]))
                {
                    i++;
                    continue;
                }

                switch (input[i])
                {
                    case '(':
                        result.Add(new Tok(Tk.LParen, "("));
                        i++;
                        break;

                    case ')':
                        result.Add(new Tok(Tk.RParen, ")"));
                        i++;
                        break;

                    case '\'':
                    {
                        // Single-quoted string literal; '' is an escaped single quote.
                        i++; // skip opening quote
                        var sb = new StringBuilder();
                        while (i < input.Length)
                        {
                            if (input[i] == '\'' && i + 1 < input.Length && input[i + 1] == '\'')
                            {
                                sb.Append('\'');
                                i += 2;
                            }
                            else if (input[i] == '\'')
                            {
                                i++;
                                break;
                            } // closing quote
                            else
                            {
                                sb.Append(input[i++]);
                            }
                        }

                        result.Add(new Tok(Tk.Str, sb.ToString()));
                        break;
                    }

                    default:
                    {
                        if (char.IsLetter(input[i]) || input[i] == '_')
                        {
                            var start = i;
                            while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
                                i++;
                            var word = input[start..i];

                            // Detect Azure Table Storage v3 typed-prefix literals.
                            if (i < input.Length && input[i] == '\'')
                            {
                                if (word.Equals("datetime", StringComparison.OrdinalIgnoreCase))
                                {
                                    i++; // skip opening quote
                                    var vs = i;
                                    while (i < input.Length && input[i] != '\'') i++;
                                    var dtStr = input[vs..i];
                                    if (i < input.Length) i++; // skip closing quote
                                    result.Add(new Tok(Tk.Dt, dtStr));
                                    break;
                                }

                                if (word.Equals("guid", StringComparison.OrdinalIgnoreCase))
                                {
                                    i++; // skip opening quote
                                    var vs = i;
                                    while (i < input.Length && input[i] != '\'') i++;
                                    var gStr = input[vs..i];
                                    if (i < input.Length) i++; // skip closing quote
                                    result.Add(new Tok(Tk.Guid, gStr));
                                    break;
                                }
                            }

                            result.Add(new Tok(Tk.Ident, word));
                        }
                        else if (char.IsDigit(input[i]) ||
                                 (input[i] == '-' && i + 1 < input.Length && char.IsDigit(input[i + 1])))
                        {
                            // Numeric literal — optional leading minus, followed by digits,
                            // with optional L/l suffix for int64.
                            var start = i;
                            if (input[i] == '-') i++;
                            while (i < input.Length && char.IsDigit(input[i])) i++;
                            var isLong = i < input.Length && input[i] is 'L' or 'l';
                            if (isLong) i++;
                            var numText = input[start..(isLong ? i - 1 : i)];
                            result.Add(new Tok(isLong ? Tk.Long : Tk.Int, numText));
                        }
                        else
                        {
                            i++; // skip unrecognised character
                        }

                        break;
                    }
                }
            }

            result.Add(new Tok(Tk.Eof, string.Empty));
            return result;
        }
    }

    // ═══════════════════════════════ PARSER ══════════════════════════════
    //
    // Grammar (in order of precedence, lowest first):
    //   expr       := or_expr
    //   or_expr    := and_expr ('or'  and_expr)*
    //   and_expr   := not_expr ('and' not_expr)*
    //   not_expr   := 'not' primary | primary
    //   primary    := '(' expr ')' | comparison
    //   comparison := Ident comparator literal
    //   comparator := eq | ne | gt | ge | lt | le

    private sealed class Parser(List<Tok> tokens)
    {
        private int _pos;

        private Tok Current => _pos < tokens.Count ? tokens[_pos] : tokens[^1];

        private Tok Consume() => tokens[_pos < tokens.Count ? _pos++ : _pos];

        private bool TryKeyword(string kw)
        {
            if (Current.Kind != Tk.Ident || !Current.Text.Equals(kw, StringComparison.OrdinalIgnoreCase)) return false;
            
            _pos++;
            return true;

        }

        public ODataExpr ParseExpression() => ParseOr();

        private ODataExpr ParseOr()
        {
            var left = ParseAnd();
            while (TryKeyword("or"))
                left = new LogicalExpr(left, isAnd: false, ParseAnd());
            return left;
        }

        private ODataExpr ParseAnd()
        {
            var left = ParseNot();
            while (TryKeyword("and"))
                left = new LogicalExpr(left, isAnd: true, ParseNot());
            return left;
        }

        private ODataExpr ParseNot()
        {
            if (TryKeyword("not"))
                return new NotExpr(ParsePrimary());
            return ParsePrimary();
        }

        private ODataExpr ParsePrimary()
        {
            if (Current.Kind == Tk.LParen)
            {
                Consume(); // (
                var inner = ParseOr();
                if (Current.Kind == Tk.RParen) Consume(); // )
                return inner;
            }

            return ParseComparison();
        }

        private ODataExpr ParseComparison()
        {
            var prop = Consume().Text;
            var opTok = Consume();
            var op = opTok.Text.ToLowerInvariant() switch
            {
                "eq" => CompOp.Eq,
                "ne" => CompOp.Ne,
                "gt" => CompOp.Gt,
                "ge" => CompOp.Ge,
                "lt" => CompOp.Lt,
                "le" => CompOp.Le,
                _ => CompOp.Eq
            };
            var val = ParseVal();
            return new CompareExpr(prop, op, val);
        }

        private ODataVal ParseVal()
        {
            var t = Consume();
            return t.Kind switch
            {
                Tk.Str => new StrVal(t.Text),
                Tk.Int when int.TryParse(t.Text, out var i) => new I32Val(i),
                Tk.Long when long.TryParse(t.Text, out var l) => new I64Val(l),
                Tk.Dt when DateTimeOffset.TryParse(
                        t.Text, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                    => new DtVal(dt),
                Tk.Guid when Guid.TryParse(t.Text, out var g) => new GuidVal(g),
                Tk.Ident when t.Text.Equals("true", StringComparison.OrdinalIgnoreCase) => new BoolVal(true),
                Tk.Ident when t.Text.Equals("false", StringComparison.OrdinalIgnoreCase) => new BoolVal(false),
                Tk.Ident when t.Text.Equals("null", StringComparison.OrdinalIgnoreCase) => NullVal.Instance,
                _ => new StrVal(t.Text) // fallback: treat unknown token text as a string
            };
        }
    }

    // ═══════════════════════════════ EVALUATOR ════════════════════════════

    private static class Evaluator
    {
        public static bool Evaluate(ODataExpr expr, JsonObject entity) => expr switch
        {
            LogicalExpr { IsAnd: true } le => Evaluate(le.Left, entity) && Evaluate(le.Right, entity),
            LogicalExpr le => Evaluate(le.Left, entity) || Evaluate(le.Right, entity),
            NotExpr ne => !Evaluate(ne.Operand, entity),
            CompareExpr ce => EvalCmp(ce, entity),
            _ => false
        };

        private static bool EvalCmp(CompareExpr ce, JsonObject entity)
        {
            if (!entity.TryGetPropertyValue(ce.Property, out var node) || node is null)
            {
                // Property absent or JSON null: only eq null is true.
                return ce is { Value: NullVal, Op: CompOp.Eq };
            }

            try
            {
                return ce.Value switch
                {
                    StrVal sv => CmpStr(ce.Op, node, sv.V),
                    I32Val iv => CmpNum(ce.Op, node, iv.V),
                    I64Val lv => CmpNum(ce.Op, node, lv.V),
                    BoolVal bv => CmpBool(ce.Op, node, bv.V),
                    DtVal dv => CmpDt(ce.Op, node, dv.V),
                    GuidVal gv => CmpGuid(ce.Op, node, gv.V),
                    NullVal => false, // property is present and non-null
                    _ => false
                };
            }
            catch
            {
                return false; // type mismatch or conversion error — don't match
            }
        }

        private static bool CmpStr(CompOp op, JsonNode node, string v)
        {
            var actual = node.GetValueKind() == JsonValueKind.String
                ? node.GetValue<string>()
                : node.ToJsonString();
            return Apply(op, string.Compare(actual, v, StringComparison.Ordinal));
        }

        private static bool CmpNum(CompOp op, JsonNode node, long v)
        {
            if (node.GetValueKind() != JsonValueKind.Number) return false;
            // JSON numbers land as long (or may be boxed as int); GetValue<long>() handles both.
            var actual = node.GetValue<long>();
            return Apply(op, actual.CompareTo(v));
        }

        private static bool CmpBool(CompOp op, JsonNode node, bool v)
        {
            var kind = node.GetValueKind();
            bool actual;
            switch (kind)
            {
                case JsonValueKind.True:
                    actual = true;
                    break;
                case JsonValueKind.False:
                    actual = false;
                    break;
                case JsonValueKind.String when
                    bool.TryParse(node.GetValue<string>(), out var b):
                    actual = b;
                    break;
                default:
                    return false;
            }

            return op == CompOp.Eq
                ? actual == v
                : op == CompOp.Ne && actual != v;
        }

        private static bool CmpDt(CompOp op, JsonNode node, DateTimeOffset v)
        {
            if (node.GetValueKind() != JsonValueKind.String) return false;
            return DateTimeOffset.TryParse(
                node.GetValue<string>(), null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var actual) && Apply(op, actual.CompareTo(v));
        }

        private static bool CmpGuid(CompOp op, JsonNode node, Guid v)
        {
            if (node.GetValueKind() != JsonValueKind.String) return false;
            return Guid.TryParse(node.GetValue<string>(), out var actual) && Apply(op, actual.CompareTo(v));
        }

        private static bool Apply(CompOp op, int cmp) => op switch
        {
            CompOp.Eq => cmp == 0,
            CompOp.Ne => cmp != 0,
            CompOp.Gt => cmp > 0,
            CompOp.Ge => cmp >= 0,
            CompOp.Lt => cmp < 0,
            CompOp.Le => cmp <= 0,
            _ => false
        };
    }
}