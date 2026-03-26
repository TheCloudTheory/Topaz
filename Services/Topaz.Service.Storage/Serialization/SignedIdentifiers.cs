using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Serialization;

public sealed class SignedIdentifiers : IXmlSerializable
{
    private TableSignedIdentifier[]? Identifiers { get; }

    [UsedImplicitly]
    [Obsolete("This constructor is only used by the serializer", true)]
    public SignedIdentifiers()
    {
    }

    public SignedIdentifiers(TableSignedIdentifier[] identifiers)
    {
        Identifiers = identifiers;
    }
    
    public XmlSchema? GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
    }

    public void WriteXml(XmlWriter writer)
    {
        if (Identifiers is null) return;
        
        foreach (var identifier in Identifiers)
        {
            writer.WriteStartElement("SignedIdentifier");
            writer.WriteElementString("Id", identifier.Id);
            
            if (identifier.AccessPolicy.StartsOn != null)
            {
                writer.WriteStartElement("Start");
                writer.WriteValue(identifier.AccessPolicy.StartsOn.Value);
                writer.WriteEndElement();
            }
            
            if (identifier.AccessPolicy.ExpiresOn != null)
            {
                writer.WriteStartElement("Expiry");
                writer.WriteValue(identifier.AccessPolicy.ExpiresOn.Value);
                writer.WriteEndElement();
            }
            
            if (identifier.AccessPolicy.Permission  != null)
            {
                writer.WriteStartElement("Permission");
                writer.WriteValue(identifier.AccessPolicy.Permission );
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement();
        }
    }
}