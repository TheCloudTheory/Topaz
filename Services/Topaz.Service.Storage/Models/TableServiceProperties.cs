// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Data.Tables.Models;

namespace Topaz.Service.Storage.Models;

public class TableServiceProperties
{
    [Obsolete("This constructor is for serialization only")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TableServiceProperties()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    /// <summary> Initializes a new instance of <see cref="TableServiceProperties"/>. </summary>
    /// <param name="logging"> Azure Analytics Logging settings. </param>
    /// <param name="hourMetrics"> A summary of request statistics grouped by API in hourly aggregates for tables. </param>
    /// <param name="minuteMetrics"> A summary of request statistics grouped by API in minute aggregates for tables. </param>
    /// <param name="cors"> The set of CORS rules. </param>
    internal TableServiceProperties(TableAnalyticsLoggingSettings logging, TableMetrics hourMetrics, TableMetrics minuteMetrics, IList<TableCorsRule> cors)
    {
        Logging = logging;
        HourMetrics = hourMetrics;
        MinuteMetrics = minuteMetrics;
        Cors = cors;
    }

    /// <summary> Azure Analytics Logging settings. </summary>
    public TableAnalyticsLoggingSettings Logging { get; set; }
    /// <summary> A summary of request statistics grouped by API in hourly aggregates for tables. </summary>
    public TableMetrics HourMetrics { get; set; }
    /// <summary> A summary of request statistics grouped by API in minute aggregates for tables. </summary>
    public TableMetrics MinuteMetrics { get; set; }
    /// <summary> The set of CORS rules. </summary>
    public IList<TableCorsRule> Cors { get; }
}