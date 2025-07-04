// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Topaz.Service.Storage.Models;

public class TableRetentionPolicy
{
    [Obsolete("This constructor is for serialization only")]
    public TableRetentionPolicy()
    {
    }
    
    public TableRetentionPolicy(bool enabled)
    {
        Enabled = enabled;
    }

    /// <summary> Initializes a new instance of <see cref="TableRetentionPolicy"/>. </summary>
    /// <param name="enabled"> Indicates whether a retention policy is enabled for the service. </param>
    /// <param name="days"> Indicates the number of days that metrics or logging or soft-deleted data should be retained. All data older than this value will be deleted. </param>
    internal TableRetentionPolicy(bool enabled, int? days)
    {
        Enabled = enabled;
        Days = days;
    }

    /// <summary> Indicates whether a retention policy is enabled for the service. </summary>
    public bool Enabled { get; set; }
    /// <summary> Indicates the number of days that metrics or logging or soft-deleted data should be retained. All data older than this value will be deleted. </summary>
    public int? Days { get; set; }
}