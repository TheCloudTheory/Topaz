using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Topaz.Service.Storage.Models;

namespace Topaz.Service.Storage.Serialization;

[XmlRoot("EnumerationResults")]
public class BlobEnumerationResult : IXmlSerializable
{
    public BlobEnumerationResult()
    {
    }

    public BlobEnumerationResult(string accountName, Blob[] blobs) : this()
    {
        AccountName = accountName;
        Blobs = blobs;
    }

    private string AccountName { get; set; } = null!;
    public string? Prefix { get; set; }
    public string? Marker { get; set; }
    public int MaxResults { get; set; }

    private Blob[]? Blobs { get; set; }
    
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

        if (Blobs is not null)
        {
            writer.WriteStartElement("Blobs");
            
            foreach (var blob in Blobs)
            {
                writer.WriteStartElement("Blob");
                writer.WriteElementString("Name", blob.Name);

                if (blob.Properties is not null)
                {
                    writer.WriteStartElement("Properties");
                    writer.WriteElementString("Last-Modified", blob.Properties.LastModified.ToString());
                    writer.WriteElementString("Etag", blob.Properties.ETag.ToString());
                    writer.WriteEndElement();
                }
                
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement();
        }
    }
}