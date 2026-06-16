namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusSqlRuleFilter
{
    public string SqlExpression { get; set; } = "1=1";
    public bool RequiresPreprocessing { get; set; } = true;
    public int CompatibilityLevel { get; set; } = 20;
}

internal sealed class ServiceBusCorrelationRuleFilter
{
    public string? ContentType { get; set; }
    public string? CorrelationId { get; set; }
    public string? MessageId { get; set; }
    public string? ReplyTo { get; set; }
    public string? ReplyToSessionId { get; set; }
    public string? SessionId { get; set; }
    public string? Subject { get; set; }
    public string? To { get; set; }
    public bool RequiresPreprocessing { get; set; } = true;
    public Dictionary<string, string>? Properties { get; set; }
}

internal sealed class ServiceBusSqlRuleAction
{
    public string? SqlExpression { get; set; }
    public bool RequiresPreprocessing { get; set; }
    public int CompatibilityLevel { get; set; } = 20;
}

internal sealed class ServiceBusRuleResourceProperties
{
    /// <summary>Filter type: "SqlFilter", "CorrelationFilter", or "True"</summary>
    public string FilterType { get; set; } = "SqlFilter";
    public ServiceBusSqlRuleFilter? SqlFilter { get; set; }
    public ServiceBusCorrelationRuleFilter? CorrelationFilter { get; set; }
    public ServiceBusSqlRuleAction? Action { get; set; }

    public static ServiceBusRuleResourceProperties DefaultTrueFilter() =>
        new()
        {
            FilterType = "True",
            SqlFilter = null,
            CorrelationFilter = null,
            Action = new ServiceBusSqlRuleAction()
        };

    public static ServiceBusRuleResourceProperties FromSqlFilter(string sqlExpression) =>
        new()
        {
            FilterType = "SqlFilter",
            SqlFilter = new ServiceBusSqlRuleFilter { SqlExpression = sqlExpression },
            Action = new ServiceBusSqlRuleAction()
        };

    public static ServiceBusRuleResourceProperties FromCorrelationFilter(
        string? contentType, string? correlationId, string? messageId,
        string? replyTo, string? replyToSessionId, string? sessionId,
        string? subject, string? to) =>
        new()
        {
            FilterType = "CorrelationFilter",
            CorrelationFilter = new ServiceBusCorrelationRuleFilter
            {
                ContentType = contentType,
                CorrelationId = correlationId,
                MessageId = messageId,
                ReplyTo = replyTo,
                ReplyToSessionId = replyToSessionId,
                SessionId = sessionId,
                Subject = subject,
                To = to
            },
            Action = new ServiceBusSqlRuleAction()
        };
}
