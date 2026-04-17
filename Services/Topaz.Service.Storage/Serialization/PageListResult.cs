using System.Xml.Serialization;
using Topaz.Service.Storage.Models;

namespace Topaz.Service.Storage.Serialization;

[XmlRoot("PageList")]
public sealed class PageListResult
{
    [XmlElement("PageRange")]
    public List<PageRangeElement> PageRanges { get; init; } = [];

    public static PageListResult From(IEnumerable<BlobPageRange> pageRanges)
        => new()
        {
            PageRanges = pageRanges.Select(range => new PageRangeElement
            {
                Start = range.Start,
                End = range.End,
            }).ToList(),
        };
}

public sealed class PageRangeElement
{
    public long Start { get; init; }
    public long End { get; init; }
}
