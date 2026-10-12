using Topaz.Service.Shared;

namespace Topaz.Service.ContainerRegistry.Models;

internal sealed class TaskUserCredentials : TopazApiModel
{
    public string? Username { get; private set; }
    public string? Password { get; private init; }
    
    public static TaskUserCredentials Create(string registryName)
    {
        return new TaskUserCredentials
        {
            Username = registryName,
            Password = Guid.NewGuid().ToString("N")
        };
    }
}