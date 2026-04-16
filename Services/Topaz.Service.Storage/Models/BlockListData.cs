namespace Topaz.Service.Storage.Models;

internal sealed record BlockListData(IReadOnlyList<BlockRecord> Committed, IReadOnlyList<BlockRecord> Uncommitted);
