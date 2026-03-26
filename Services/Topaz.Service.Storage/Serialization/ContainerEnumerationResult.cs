using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Topaz.Service.Storage.Models;

namespace Topaz.Service.Storage.Serialization;

[XmlRoot("EnumerationResults")]
public class ContainerEnumerationResult : IXmlSerializable
{
    public ContainerEnumerationResult()
    {
    }

    public ContainerEnumerationResult(string accountName, Container[] containers) : this()
    {
        AccountName = accountName;
        Containers = containers;
    }

    private string AccountName { get; set; } = null!;
    public string? Prefix { get; set; }
    public string? Marker { get; set; }
    public int MaxResults { get; set; }

    private Container[]? Containers { get; set; }
    
    public XmlSchema? GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("ServiceEndpoint", $"https://{AccountName}.blob.core.windows.net");
        writer.WriteElementString("Prefix", Prefix);
        writer.WriteElementString("Marker", Marker);
        writer.WriteElementString("MaxResults", MaxResults.ToString());

        if (Containers is not null)
        {
            writer.WriteStartElement("Containers");
            
            foreach (var container in Containers)
            {
                writer.WriteStartElement("Container");
                writer.WriteElementString("Name", container.Name);
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement();
        }
    }
}