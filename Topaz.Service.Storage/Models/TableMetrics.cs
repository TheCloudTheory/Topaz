// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Data.Tables;

namespace Topaz.Service.Storage.Models;

public class TableMetrics
{
    [Obsolete("This constructor is for serialization only")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TableMetrics()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }
    
    [Obsolete("This constructor is for serialization only")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TableMetrics(bool enabled)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        Enabled = enabled;
    }

    /// <summary> Initializes a new instance of <see cref="TableMetrics"/>. </summary>
    /// <param name="version"> The version of Analytics to configure. </param>
    /// <param name="enabled"> Indicates whether metrics are enabled for the Table service. </param>
    /// <param name="includeApis"> Indicates whether metrics should generate summary statistics for called API operations. </param>
    /// <param name="retentionPolicy"> The retention policy. </param>
    internal TableMetrics(string version, bool enabled, bool? includeApis, TableRetentionPolicy retentionPolicy)
    {
        Version = version;
        Enabled = enabled;
        IncludeApis = includeApis;
        RetentionPolicy = retentionPolicy;
    }

    /// <summary> The version of Analytics to configure. </summary>
    public string Version { get; set; }
    /// <summary> Indicates whether metrics are enabled for the Table service. </summary>
    public bool Enabled { get; set; }
    /// <summary> The retention policy. </summary>
    public TableRetentionPolicy RetentionPolicy { get; set; }
    public bool? IncludeApis { get; set; }
}