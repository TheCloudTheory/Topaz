namespace Topaz.Service.Shared;

public interface IServiceDefinition
{
    string Name { get; }
    static abstract bool IsGlobalService { get; }

    static abstract string LocalDirectoryPath { get; }

    IReadOnlyCollection<IEndpointDefinition> Endpoints { get; }
    static abstract IReadOnlyCollection<string>? Subresources { get; }
}
