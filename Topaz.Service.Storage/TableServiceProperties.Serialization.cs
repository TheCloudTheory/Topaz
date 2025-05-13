// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System.Xml.Linq;
using Azure.Data.Tables.Models;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;
using TableMetrics = Topaz.Service.Storage.Models.TableMetrics;
using TableRetentionPolicy = Topaz.Service.Storage.Models.TableRetentionPolicy;
using TableAnalyticsLoggingSettings = Topaz.Service.Storage.Models.TableAnalyticsLoggingSettings;

namespace Topaz.Service.Storage;

public static class TableServicePropertiesSerialization
{
    internal static TableServiceProperties DeserializeTableServiceProperties(XElement element)
    {
        TableAnalyticsLoggingSettings logging = null;
        TableMetrics hourMetrics = null;
        TableMetrics minuteMetrics = null;
        IList<TableCorsRule> cors = null;
        
        if (element.Element("Logging") is { } loggingElement)
        {
            logging = DeserializeTableAnalyticsLoggingSettings(loggingElement);
        }

        if (element.Element("HourMetrics") is { } hourMetricsElement)
        {
            hourMetrics = DeserializeTableMetrics(hourMetricsElement);
        }

        if (element.Element("MinuteMetrics") is { } minuteMetricsElement)
        {
            minuteMetrics = DeserializeTableMetrics(minuteMetricsElement);
        }

        if (element.Element("Cors") is not { } corsElement)
            return new TableServiceProperties(logging, hourMetrics, minuteMetrics, cors);
        
        var array = corsElement.Elements("CorsRule").Select(DeserializeTableCorsRule).ToList();

        cors = array;

        return new TableServiceProperties(logging, hourMetrics, minuteMetrics, cors);
    }

    private static TableAnalyticsLoggingSettings DeserializeTableAnalyticsLoggingSettings(XElement element)
    {
        string version = default;
        bool delete = default;
        bool read = default;
        bool write = default;
        TableRetentionPolicy retentionPolicy = default;
        if (element.Element("Version") is XElement versionElement)
        {
            version = (string)versionElement;
        }
        if (element.Element("Delete") is XElement deleteElement)
        {
            delete = (bool)deleteElement;
        }
        if (element.Element("Read") is XElement readElement)
        {
            read = (bool)readElement;
        }
        if (element.Element("Write") is XElement writeElement)
        {
            write = (bool)writeElement;
        }
        if (element.Element("RetentionPolicy") is XElement retentionPolicyElement)
        {
            retentionPolicy = DeserializeTableRetentionPolicy(retentionPolicyElement);
        }
        return new TableAnalyticsLoggingSettings(version, delete, read, write, retentionPolicy);
    }

    private static TableMetrics DeserializeTableMetrics(XElement element)
    {
        string version = default;
        bool enabled = default;
        bool? includeApis = default;
        TableRetentionPolicy retentionPolicy = default;
        if (element.Element("Version") is XElement versionElement)
        {
            version = (string)versionElement;
        }
        if (element.Element("Enabled") is XElement enabledElement)
        {
            enabled = (bool)enabledElement;
        }
        if (element.Element("IncludeAPIs") is XElement includeAPIsElement)
        {
            includeApis = (bool?)includeAPIsElement;
        }
        if (element.Element("RetentionPolicy") is XElement retentionPolicyElement)
        {
            retentionPolicy = DeserializeTableRetentionPolicy(retentionPolicyElement);
        }
        return new TableMetrics(version, enabled, includeApis, retentionPolicy);
    }
    
    internal static TableCorsRule DeserializeTableCorsRule(XElement element)
    {
        string allowedOrigins = default;
        string allowedMethods = default;
        string allowedHeaders = default;
        string exposedHeaders = default;
        int maxAgeInSeconds = default;
        if (element.Element("AllowedOrigins") is XElement allowedOriginsElement)
        {
            allowedOrigins = (string)allowedOriginsElement;
        }
        if (element.Element("AllowedMethods") is XElement allowedMethodsElement)
        {
            allowedMethods = (string)allowedMethodsElement;
        }
        if (element.Element("AllowedHeaders") is XElement allowedHeadersElement)
        {
            allowedHeaders = (string)allowedHeadersElement;
        }
        if (element.Element("ExposedHeaders") is XElement exposedHeadersElement)
        {
            exposedHeaders = (string)exposedHeadersElement;
        }
        if (element.Element("MaxAgeInSeconds") is XElement maxAgeInSecondsElement)
        {
            maxAgeInSeconds = (int)maxAgeInSecondsElement;
        }
        return new TableCorsRule(allowedOrigins, allowedMethods, allowedHeaders, exposedHeaders, maxAgeInSeconds);
    }
    
    internal static TableRetentionPolicy DeserializeTableRetentionPolicy(XElement element)
    {
        bool enabled = default;
        int? days = default;
        if (element.Element("Enabled") is XElement enabledElement)
        {
            enabled = (bool)enabledElement;
        }
        if (element.Element("Days") is XElement daysElement)
        {
            days = (int?)daysElement;
        }
        return new TableRetentionPolicy(enabled, days);
    }
}