using Amqp;

namespace Topaz.Host.AMQP.Filtering;

/// <summary>
/// Evaluates a SQL-92 subset expression against an AMQP message's system properties
/// and user application properties.
///
/// Supported grammar:
///   expr     = or_expr
///   or_expr  = and_expr  ( 'OR'  and_expr  )*
///   and_expr = not_expr  ( 'AND' not_expr  )*
///   not_expr = 'NOT' not_expr | primary
///   primary  = '(' expr ')' | comparison | null_check
///   null_check   = name 'IS' ['NOT'] 'NULL'
///   comparison   = value op value
///   op       = '=' | '!=' | '&lt;&gt;' | '&lt;' | '&gt;' | '&lt;=' | '>='
///   value    = name | string_literal | number | 'true' | 'false'
///   name     = identifier | 'sys.' identifier
/// </summary>
internal static class SqlFilterParser
{
    public static bool Evaluate(string expression, Message message)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true;

        var tokens = Tokenize(expression);
        var pos = 0;
        var result = ParseOr(tokens, ref pos, message);
        return result;
    }

    // ── Tokenizer ────────────────────────────────────────────────────────────

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < input.Length)
        {
            if (char.IsWhiteSpace(input[i])) { i++; continue; }

            // String literal
            if (input[i] == '\'')
            {
                var sb = new System.Text.StringBuilder();
                i++; // skip opening quote
                while (i < input.Length)
                {
                    if (input[i] == '\'' && i + 1 < input.Length && input[i + 1] == '\'')
                    {
                        // Escaped quote
                        sb.Append('\'');
                        i += 2;
                    }
                    else if (input[i] == '\'')
                    {
                        i++; break;
                    }
                    else
                    {
                        sb.Append(input[i++]);
                    }
                }
                tokens.Add("'" + sb + "'");
                continue;
            }

            // Two-character operators
            if (i + 1 < input.Length)
            {
                var two = input.Substring(i, 2);
                if (two is "!=" or "<>" or "<=" or ">=")
                {
                    tokens.Add(two);
                    i += 2;
                    continue;
                }
            }

            // Single-character operators and parentheses
            if ("=<>()".Contains(input[i]))
            {
                tokens.Add(input[i].ToString());
                i++;
                continue;
            }

            // Number
            if (char.IsDigit(input[i]) || (input[i] == '-' && i + 1 < input.Length && char.IsDigit(input[i + 1])))
            {
                var sb = new System.Text.StringBuilder();
                if (input[i] == '-') sb.Append(input[i++]);
                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'))
                    sb.Append(input[i++]);
                tokens.Add(sb.ToString());
                continue;
            }

            // Identifier / keyword
            if (char.IsLetter(input[i]) || input[i] == '_')
            {
                var sb = new System.Text.StringBuilder();
                while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] is '_' or '.'))
                    sb.Append(input[i++]);
                tokens.Add(sb.ToString());
                continue;
            }

            // Skip unknown character
            i++;
        }
        return tokens;
    }

    // ── Parser ───────────────────────────────────────────────────────────────

    private static bool ParseOr(List<string> t, ref int pos, Message msg)
    {
        var left = ParseAnd(t, ref pos, msg);
        while (pos < t.Count && t[pos].Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            pos++;
            var right = ParseAnd(t, ref pos, msg);
            left = left || right;
        }
        return left;
    }

    private static bool ParseAnd(List<string> t, ref int pos, Message msg)
    {
        var left = ParseNot(t, ref pos, msg);
        while (pos < t.Count && t[pos].Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            pos++;
            var right = ParseNot(t, ref pos, msg);
            left = left && right;
        }
        return left;
    }

    private static bool ParseNot(List<string> t, ref int pos, Message msg)
    {
        if (pos < t.Count && t[pos].Equals("NOT", StringComparison.OrdinalIgnoreCase))
        {
            pos++;
            return !ParseNot(t, ref pos, msg);
        }
        return ParsePrimary(t, ref pos, msg);
    }

    private static bool ParsePrimary(List<string> t, ref int pos, Message msg)
    {
        if (pos < t.Count && t[pos] == "(")
        {
            pos++; // consume '('
            var result = ParseOr(t, ref pos, msg);
            if (pos < t.Count && t[pos] == ")") pos++; // consume ')'
            return result;
        }

        return ParseComparison(t, ref pos, msg);
    }

    private static bool ParseComparison(List<string> t, ref int pos, Message msg)
    {
        var left = ParseValue(t, ref pos, msg);

        // IS [NOT] NULL
        if (pos < t.Count && t[pos].Equals("IS", StringComparison.OrdinalIgnoreCase))
        {
            pos++;
            var negate = false;
            if (pos < t.Count && t[pos].Equals("NOT", StringComparison.OrdinalIgnoreCase))
            {
                negate = true;
                pos++;
            }
            if (pos < t.Count && t[pos].Equals("NULL", StringComparison.OrdinalIgnoreCase)) pos++;
            var isNull = left is null;
            return negate ? !isNull : isNull;
        }

        if (pos >= t.Count) return left is bool b && b;

        var op = t[pos];
        if (op is not ("=" or "!=" or "<>" or "<" or ">" or "<=" or ">="))
            return left is bool bv && bv;

        pos++;
        var right = ParseValue(t, ref pos, msg);

        return Compare(left, op, right);
    }

    private static object? ParseValue(List<string> t, ref int pos, Message msg)
    {
        if (pos >= t.Count) return null;

        var tok = t[pos++];

        // String literal
        if (tok.StartsWith("'") && tok.EndsWith("'"))
            return tok.Substring(1, tok.Length - 2);

        // Boolean literals
        if (tok.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (tok.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;

        // Numeric literals
        if (double.TryParse(tok, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;

        // Property access — sys.* prefix = system properties, otherwise application properties
        return ResolveProperty(tok, msg);
    }

    // ── Property resolution ──────────────────────────────────────────────────

    private static object? ResolveProperty(string name, Message msg)
    {
        if (name.StartsWith("sys.", StringComparison.OrdinalIgnoreCase))
        {
            var sysName = name.Substring(4);
            return ResolveSystemProperty(sysName, msg);
        }

        // Application property
        if (msg.ApplicationProperties == null) return null;
        return msg.ApplicationProperties[name];
    }

    private static object? ResolveSystemProperty(string name, Message msg)
    {
        if (msg.Properties == null) return null;
        return name.ToLowerInvariant() switch
        {
            "messageid"          => msg.Properties.MessageId,
            "correlationid"      => msg.Properties.CorrelationId,
            "contenttype"        => msg.Properties.ContentType?.ToString(),
            "subject" or "label" => msg.Properties.Subject,
            "to"                 => msg.Properties.To,
            "replyto"            => msg.Properties.ReplyTo,
            "sessionid"          => msg.Properties.GroupId,
            "replytosessionid"   => msg.Properties.ReplyToGroupId,
            _                    => null
        };
    }

    // ── Comparator ──────────────────────────────────────────────────────────

    private static bool Compare(object? left, string op, object? right)
    {
        if (left is null || right is null)
            return false;

        // Numeric comparison: coerce both to double when possible
        var leftD = ToDouble(left);
        var rightD = ToDouble(right);
        if (leftD.HasValue && rightD.HasValue)
        {
            return op switch
            {
                "="        => leftD.Value == rightD.Value,
                "!=" or "<>" => leftD.Value != rightD.Value,
                "<"        => leftD.Value < rightD.Value,
                ">"        => leftD.Value > rightD.Value,
                "<="       => leftD.Value <= rightD.Value,
                ">="       => leftD.Value >= rightD.Value,
                _          => false
            };
        }

        // String / object equality
        var leftStr  = left.ToString();
        var rightStr = right.ToString();
        return op switch
        {
            "="          => string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
            "!=" or "<>" => !string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
            _            => false
        };
    }

    private static double? ToDouble(object? value)
    {
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is float f) return f;
        if (value is decimal dec) return (double)dec;
        if (value is string s && double.TryParse(s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }
}
