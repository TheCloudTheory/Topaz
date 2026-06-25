using Topaz.Chaos.Endpoints;
using Topaz.Chaos.Endpoints.Rules;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos;

public sealed class ChaosService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => string.Empty;
    public static IReadOnlyCollection<string>? Subresources => [];
    public static string UniqueName => "chaos";
    public string Name => "Chaos";
    public bool IsTopazService => true;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new EnableChaosEndpoint(logger),
        new DisableChaosEndpoint(logger),
        new GetChaosStatusEndpoint(logger),
        new CreateChaosRuleEndpoint(logger),
        new GetChaosRuleEndpoint(logger),
        new DeleteChaosRuleEndpoint(logger),
        new ListChaosRulesEndpoint(logger),
        new EnableChaosRuleEndpoint(logger),
        new DisableChaosRuleEndpoint(logger)
    ];
}