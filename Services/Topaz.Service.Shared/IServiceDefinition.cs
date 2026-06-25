namespace Topaz.Service.Shared;

public interface IServiceDefinition
{
    static abstract bool IsGlobalService { get; }
    static abstract string LocalDirectoryPath { get; }
    static abstract IReadOnlyCollection<string>? Subresources { get; }
    static abstract string UniqueName { get; }
    
    string Name { get; }
    IReadOnlyCollection<IEndpointDefinition> Endpoints { get; }

    /// <summary>
    /// Register event handlers. Called for ALL services before any Initialize() runs,
    /// guaranteeing that no event is fired before its handler is registered.
    /// </summary>
    void Register() { }

    /// <summary>
    /// Run side-effectful initialization: create directories, seed data, fire events.
    /// Called for all services after all Register() calls have completed.
    /// </summary>
    void Initialize() { }

    /// <summary>
    /// Indicates whether this service is a Topaz service. Used to distinguish between
    /// infrastructure services (e.g., FinsOps, Chaus) and azure-defined
    /// services (e.g., Key Vault, Storage).
    /// </summary>
    public bool IsTopazService => false;
}
