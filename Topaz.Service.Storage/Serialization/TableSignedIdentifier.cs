using System.Xml.Linq;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Serialization;

public sealed class TableSignedIdentifier
{
    [UsedImplicitly]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TableSignedIdentifier()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public TableSignedIdentifier(string id, TableAccessPolicy accessPolicy)
    {
        this.Id = id;
        this.AccessPolicy = accessPolicy;
    }
    
    public string Id { get; set; }
    public TableAccessPolicy AccessPolicy { get; set; }

    internal static TableSignedIdentifier DeserializeTableSignedIdentifier(XElement element)
    {
        string? id = null;
        TableAccessPolicy? accessPolicy = null;
        if (element.Element("Id") is {} idElement)
        {
            id = (string)idElement;
        }
        
        if (element.Element("AccessPolicy") is {} accessPolicyElement)
        {
            accessPolicy = TableAccessPolicy.DeserializeTableAccessPolicy(accessPolicyElement);
        }
        
        return new TableSignedIdentifier(id!, accessPolicy!);
    }
}