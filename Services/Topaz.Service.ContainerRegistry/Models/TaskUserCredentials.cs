using Topaz.Service.Shared;

namespace Topaz.Service.ContainerRegistry.Models;

internal sealed class TaskUserCredentials : TopazApiModel
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    
    public static TaskUserCredentials Create(string registryName)
    {
        return new TaskUserCredentials
        {
            Username = registryName,
            Password = Guid.NewGuid().ToString("N")
        };
    }
}