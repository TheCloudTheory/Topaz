// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Topaz.Service.Storage.Models;

public class TableAnalyticsLoggingSettings
{
    public TableAnalyticsLoggingSettings(string version, bool delete, bool read, bool write, TableRetentionPolicy retentionPolicy)
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