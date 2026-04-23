using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Topaz.Service.Storage.Models;

namespace Topaz.Service.Storage.Serialization;

[XmlRoot("EnumerationResults")]
public class QueueEnumerationResult : IXmlSerializable
{
    public QueueEnumerationResult()
    {
    }

    public QueueEnumerationResult(string accountName, QueueProperties[] queues) : this()
    {
        AccountName = accountName;
        Queues = queues;
    }

    private string AccountName { get; set; } = null!;
    public string? Prefix { get; set; }
    public string? Marker { get; set; }
    public int MaxResults { get; set; }

    private QueueProperties[]? Queues { get; set; }

    public QueueProperties[] GetQueues() => Queues ?? [];

    public XmlSchema? GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("ServiceEndpoint", $"https://{AccountName}.queue.core.windows.net");
        writer.WriteElementString("Prefix", Prefix);
        writer.WriteElementString("Marker", Marker);
        writer.WriteElementString("MaxResults", MaxResults.ToString());

        if (Queues is not null)
        {
            writer.WriteStartElement("Queues");
            
            foreach (var queue in Queues)
            {
                writer.WriteStartElement("Queue");
                writer.WriteElementString("Name", queue.Name);
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement();
        }
    }
}
