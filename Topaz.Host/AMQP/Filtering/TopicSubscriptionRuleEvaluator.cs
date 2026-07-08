using Amqp;
using Topaz.Service.ServiceBus.Models;

namespace Topaz.Host.AMQP.Filtering;

/// <summary>
/// Evaluates topic subscription rule filters (TrueFilter, CorrelationFilter, SqlFilter)
/// against an AMQP message.
/// </summary>
internal static class TopicSubscriptionRuleEvaluator
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="message"/> matches <paramref name="rule"/>.
    /// </summary>
    public static bool Matches(Message message, ServiceBusRuleResourceProperties rule)
    {
        return rule.FilterType switch
        {
            "True"              => true,
            "CorrelationFilter" => MatchesCorrelation(message, rule.CorrelationFilter),
            "SqlFilter"         => MatchesSql(message, rule.SqlFilter),
            _                   => true // unknown filter type: fail-open
        };
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="message"/> matches at least one rule in
    /// <paramref name="rules"/>, or when <paramref name="rules"/> is empty (treat as TrueFilter).
    /// </summary>
    public static bool MatchesAny(Message message, ServiceBusRuleResourceProperties[] rules)
    {
        if (rules.Length == 0)
            return true;

        foreach (var rule in rules)
        {
            if (Matches(message, rule))
                return true;
        }
        return false;
    }

    // ── CorrelationFilter ────────────────────────────────────────────────────

    private static bool MatchesCorrelation(Message message, ServiceBusCorrelationRuleFilter? filter)
    {
        if (filter == null)
            return true;

        var props = message.Properties;

        if (filter.ContentType    != null && !StringMatch(filter.ContentType,    props?.ContentType?.ToString()))    return false;
        if (filter.CorrelationId  != null && !StringMatch(filter.CorrelationId,  props?.CorrelationId))              return false;
        if (filter.MessageId      != null && !StringMatch(filter.MessageId,      props?.MessageId?.ToString()))      return false;
        if (filter.ReplyTo        != null && !StringMatch(filter.ReplyTo,        props?.ReplyTo))                    return false;
        if (filter.ReplyToSessionId != null && !StringMatch(filter.ReplyToSessionId, props?.ReplyToGroupId))         return false;
        if (filter.SessionId      != null && !StringMatch(filter.SessionId,      props?.GroupId))                    return false;
        if (filter.Subject        != null && !StringMatch(filter.Subject,        props?.Subject))                    return false;
        if (filter.To             != null && !StringMatch(filter.To,             props?.To))                         return false;

        // Application properties
        if (filter.Properties != null)
        {
            foreach (var (key, expectedValue) in filter.Properties)
            {
                var actual = message.ApplicationProperties?[key]?.ToString();
                if (!StringMatch(expectedValue, actual))
                    return false;
            }
        }

        return true;
    }

    // ── SqlFilter ────────────────────────────────────────────────────────────

    private static bool MatchesSql(Message message, ServiceBusSqlRuleFilter? filter)
    {
        if (filter == null)
            return true;

        var expression = filter.SqlExpression;

        // Short-circuit the trivially true expressions used as defaults.
        if (string.IsNullOrWhiteSpace(expression) || expression.Trim() == "1=1")
            return true;

        return SqlFilterParser.Evaluate(expression, message);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool StringMatch(string? expected, string? actual)
        => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
}
