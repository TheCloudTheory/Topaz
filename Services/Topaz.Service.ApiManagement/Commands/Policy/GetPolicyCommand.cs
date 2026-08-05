using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands.Policy;

[UsedImplicitly]
[CommandDefinition("apim policy show", "api-management", "Gets a policy in an Azure API Management service.")]
[CommandExample("Gets a policy in an API Management service",
    "topaz apim policy show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --service-name \"my-apim\" \\\n    --policy-id \"policy\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetPolicyCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetPolicyCommand.GetPolicyCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GetPolicyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service/{settings.ServiceName}/policies/{settings.PolicyId}";
        var (success, body) = await GetAsync(url, cancellationToken);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, GetPolicyCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ServiceName))
            return ValidationResult.Error("Service name can't be null.");
        if (string.IsNullOrEmpty(settings.PolicyId))
            return ValidationResult.Error("Policy ID can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetPolicyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) API Management service name")]
        [CommandOption("--service-name")]
        public string? ServiceName { get; set; }

        [CommandOptionDefinition("(Required) policy identifier")]
        [CommandOption("--policy-id")]
        public string? PolicyId { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
