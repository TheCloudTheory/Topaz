namespace Topaz.Service.Shared;

public interface IServiceDefinition
{
    static abstract bool IsGlobalService { get; }
    static abstract string LocalDirectoryPath { get; }
    static abstract IReadOnlyCollection<string>? Subresources { get; }
    static abstract string UniqueName { get; }
    
    string Name { get; }
    IReadOnlyCollection<IEndpointDefinition> Endpoints { get; }
    
}
