namespace Topaz.Service.Shared;

public interface IServiceDefinition
{
    string Name { get; }

    static abstract string LocalDirectoryPath { get; }

    IReadOnlyCollection<IEndpointDefinition> Endpoints { get; }
}
