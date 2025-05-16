using System.Globalization;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Serialization;

public sealed class TableAccessPolicy
{
    [UsedImplicitly]
    public TableAccessPolicy()
    {
    }

    public TableAccessPolicy(DateTimeOffset? startsOn, DateTimeOffset? expiresOn, string? permission)
    {
        this.StartsOn = startsOn;
        this.ExpiresOn = expiresOn;
        this.Permission = permission;
    }
    
    public string? Permission { get; set; }
    public DateTimeOffset? StartsOn { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }

    internal static TableAccessPolicy DeserializeTableAccessPolicy(XElement element)
    {
        DateTimeOffset? startsOn = null;
        DateTimeOffset? expiresOn = null;
        string? permission = null;
        if (element.Element("Start") is { } startElement)
        {
            startsOn = GetDateTimeOffsetValue(startElement,"O");
        }
        
        if (element.Element("Expiry") is { } expiryElement)
        {
            expiresOn = GetDateTimeOffsetValue(expiryElement,"O");
        }
        
        if (element.Element("Permission") is { } permissionElement)
        {
            permission = (string)permissionElement;
        }
        
        return new TableAccessPolicy(startsOn, expiresOn, permission);
    }
    
    private static DateTimeOffset GetDateTimeOffsetValue(XElement element, string format) => format switch
    {
        "U" => DateTimeOffset.FromUnixTimeSeconds((long)element),
        _ => ParseDateTimeOffset(element.Value, format)
    };
    
    private static DateTimeOffset ParseDateTimeOffset(string value, string format)
    {
        return format switch
        {
            "U" => DateTimeOffset.FromUnixTimeSeconds(long.Parse(value, CultureInfo.InvariantCulture)),
            _ => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
        };
    }
}