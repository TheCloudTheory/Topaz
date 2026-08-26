namespace Topaz.Service.EventGrid.Models;

internal sealed class NamespaceSharedAccessKey
{
    public string? KeyName { get; init; }
    public string? KeyValue { get; init; }
    
    public static NamespaceSharedAccessKey Generate(string keyName)
    {
        return new NamespaceSharedAccessKey
        {
            KeyName = keyName,
            KeyValue = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        };
    }
}