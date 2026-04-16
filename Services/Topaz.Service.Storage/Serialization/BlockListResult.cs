using System.Xml.Serialization;
using Topaz.Service.Storage.Models;

namespace Topaz.Service.Storage.Serialization;

[XmlRoot("BlockList")]
public sealed class BlockListResult
{
    [XmlArray("CommittedBlocks")]
    [XmlArrayItem("Block")]
    public List<BlockEntry> CommittedBlocks { get; set; } = [];

    [XmlArray("UncommittedBlocks")]
    [XmlArrayItem("Block")]
    public List<BlockEntry> UncommittedBlocks { get; set; } = [];

    internal static BlockListResult From(IReadOnlyList<BlockRecord> committed, IReadOnlyList<BlockRecord> uncommitted) =>
        new()
        {
            CommittedBlocks = committed.Select(b => new BlockEntry { Name = b.Name, Size = b.Size }).ToList(),
            UncommittedBlocks = uncommitted.Select(b => new BlockEntry { Name = b.Name, Size = b.Size }).ToList()
        };
}

public sealed class BlockEntry
{
    [XmlElement("Name")]
    public string Name { get; set; } = "";

    [XmlElement("Size")]
    public long Size { get; set; }
}
