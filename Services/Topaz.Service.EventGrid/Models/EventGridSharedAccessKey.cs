namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridSharedAccessKey
{
    public string? KeyName { get; init; }
    public string? KeyValue { get; init; }
    
    public static EventGridSharedAccessKey Generate(string keyName)
    {
        return new EventGridSharedAccessKey
        {
            KeyName = keyName,
            KeyValue = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        };
    }
}