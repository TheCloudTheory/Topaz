using Amqp;
using Amqp.Framing;
using Topaz.Service.ServiceBus.Models;

namespace Topaz.Host.AMQP.Filtering;

/// <summary>
/// Applies a <see cref="ServiceBusSqlRuleAction"/> to an AMQP message copy after a
/// filter match.  Only <c>SET key = value</c> and <c>REMOVE key</c> statements are
/// supported (the subset used by real Azure Service Bus rule actions).
/// </summary>
internal static class SqlRuleActionApplicator
{
    /// <summary>
    /// Mutates the application properties of <paramref name="message"/> according to the
    /// SQL rule action expression.  No-op when the action or its expression is empty.
    /// </summary>
    public static void Apply(ServiceBusSqlRuleAction? action, Message message)
    {
        if (action == null) return;
        var expression = action.SqlExpression;
        if (string.IsNullOrWhiteSpace(expression)) return;

        message.ApplicationProperties ??= new ApplicationProperties();

        // Split on semicolons to support multiple statements: "SET a = 1; REMOVE b"
        foreach (var rawStatement in expression.Split(';'))
        {
            var statement = rawStatement.Trim();
            if (string.IsNullOrEmpty(statement)) continue;

            if (statement.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
            {
                ApplySet(statement.Substring(4).Trim(), message);
            }
            else if (statement.StartsWith("REMOVE ", StringComparison.OrdinalIgnoreCase))
            {
                ApplyRemove(statement.Substring(7).Trim(), message);
            }
        }
    }

    // ── SET key = value ──────────────────────────────────────────────────────

    private static void ApplySet(string body, Message message)
    {
        // body = "key = value"
        var eq = body.IndexOf('=');
        if (eq < 0) return;

        var key   = body.Substring(0, eq).Trim();
        var value = ParseLiteral(body.Substring(eq + 1).Trim());

        message.ApplicationProperties[key] = value;
    }

    // ── REMOVE key ───────────────────────────────────────────────────────────

    private static void ApplyRemove(string key, Message message)
    {
        key = key.Trim();
        if (!string.IsNullOrEmpty(key))
            message.ApplicationProperties.Map.Remove(key);
    }

    // ── Literal parser ───────────────────────────────────────────────────────

    private static object? ParseLiteral(string raw)
    {
        if (raw.StartsWith("'") && raw.EndsWith("'"))
            return raw.Substring(1, raw.Length - 2).Replace("''", "'");

        if (raw.Equals("true", StringComparison.OrdinalIgnoreCase))  return true;
        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        if (raw.Equals("null", StringComparison.OrdinalIgnoreCase))  return null;

        if (long.TryParse(raw, out var l)) return l;
        if (double.TryParse(raw,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;

        return raw;
    }
}
