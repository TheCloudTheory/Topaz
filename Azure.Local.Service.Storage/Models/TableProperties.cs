namespace Azure.Local.Service.Storage.Models;

internal class TableProperties(string tableName)
{
    public string TableName { get; set; } = tableName;
}
