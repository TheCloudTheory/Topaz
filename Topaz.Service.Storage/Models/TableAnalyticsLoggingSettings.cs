// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Topaz.Service.Storage.Models;

public class TableAnalyticsLoggingSettings
{
    [Obsolete("This constructor is for serialization only")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TableAnalyticsLoggingSettings()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }
    
    internal TableAnalyticsLoggingSettings(string version, bool delete, bool read, bool write, TableRetentionPolicy retentionPolicy)
    {
        Version = version;
        Delete = delete;
        Read = read;
        Write = write;
        RetentionPolicy = retentionPolicy;
    }

    /// <summary> The version of Analytics to configure. </summary>
    public string Version { get; set; }
    /// <summary> Indicates whether all delete requests should be logged. </summary>
    public bool Delete { get; set; }
    /// <summary> Indicates whether all read requests should be logged. </summary>
    public bool Read { get; set; }
    /// <summary> Indicates whether all write requests should be logged. </summary>
    public bool Write { get; set; }
    /// <summary> The retention policy. </summary>
    public TableRetentionPolicy RetentionPolicy { get; set; }
}