using Azure.Data.Tables.Models;

namespace Azure.Local.Service.Storage;

internal sealed class TableServiceControlPlane
{
    public TableItem[] GetTables()
    {
        return
        [
            new TableItem("foo")
        ];
    }
}
