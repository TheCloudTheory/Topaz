using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
[CommandDefinition("deployment group list", "deployment", "Returns a list of all deployments for the given resource group")]
public class ListGroupDeploymentCommand(HttpClient httpClient) : TopazHttpCommand<ListGroupDeploymentCommand.ListGroupDeploymentCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListGroupDeploymentCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Resources/deployments";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListGroupDeploymentCommandSettings settings)
    {
        if(string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Group deployment subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Group deployment subscription ID must be a valid GUID.");
        }

        return string.IsNullOrEmpty(settings.ResourceGroup) ? ValidationResult.Error("Resource group name is required when creating a group deployment.") : base.Validate(context, settings);
    }
    
    [UsedImplicitly]
    public sealed class ListGroupDeploymentCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("Subscription ID for the deployment", true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("Resource group for the deployment", true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}