namespace Azure.Local.Service.Shared;

public interface IServiceDefinition
{
    string Name { get; }

    IReadOnlyCollection<IEndpointDefinition> Endpoints { get; }
}
