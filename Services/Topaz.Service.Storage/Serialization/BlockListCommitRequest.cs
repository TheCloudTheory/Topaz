using System.Xml.Linq;

namespace Topaz.Service.Storage.Serialization;

internal sealed class BlockListCommitRequest
{
    public List<string> Latest { get; private set; } = [];
    public List<string> Committed { get; private set; } = [];
    public List<string> Uncommitted { get; private set; } = [];

    public IEnumerable<string> AllBlockIds => Latest.Concat(Committed).Concat(Uncommitted);

    public static BlockListCommitRequest? Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new BlockListCommitRequest();

        try
        {
            var root = XDocument.Parse(xml).Root;
            if (root == null) return new BlockListCommitRequest();

            return new BlockListCommitRequest
            {
                Latest = root.Elements("Latest").Select(e => e.Value).ToList(),
                Committed = root.Elements("Committed").Select(e => e.Value).ToList(),
                Uncommitted = root.Elements("Uncommitted").Select(e => e.Value).ToList()
            };
        }
        catch
        {
            return null;
        }
    }
}
