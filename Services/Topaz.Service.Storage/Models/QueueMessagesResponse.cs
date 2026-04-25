using System.Xml.Serialization;

namespace Topaz.Service.Storage.Models;

/// <summary>
/// DTO for Azure Queue Storage get messages response per Azure Queue Storage API.
/// Represents: &lt;QueueMessagesList&gt;&lt;QueueMessage&gt;...&lt;/QueueMessage&gt;&lt;/QueueMessagesList&gt;
/// </summary>
[XmlRoot("QueueMessagesList")]
public class QueueMessagesResponse
{
    [XmlElement("QueueMessage")]
    public List<QueueMessageResponseItem>? Messages { get; set; }
}

/// <summary>
/// Individual message item in the QueueMessagesList response.
/// Maps to Azure Queue Storage message response fields with RFC 1123 date formatting.
/// </summary>
public class QueueMessageResponseItem
{
    [XmlElement("MessageId")]
    public string? MessageId { get; set; }

    [XmlElement("InsertionTime")]
    public string? InsertionTime { get; set; }

    [XmlElement("ExpirationTime")]
    public string? ExpirationTime { get; set; }

    [XmlElement("PopReceipt")]
    public string? PopReceipt { get; set; }

    [XmlElement("TimeNextVisible")]
    public string? TimeNextVisible { get; set; }

    [XmlElement("DequeueCount")]
    public int DequeueCount { get; set; }

    [XmlElement("MessageText")]
    public string? MessageText { get; set; }
}
