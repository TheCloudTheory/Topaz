namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusNamespaceResourceProperties
{
    public string? AlternateName { get; set; }
    public DateTimeOffset? CreatedAt { get; init; }
    public bool DisableLocalAuth { get; init; }
}