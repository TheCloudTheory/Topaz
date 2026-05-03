using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Commands;

[UsedImplicitly]
[CommandDefinition("group update", "group", "Updates the tags of a resource group.")]
[CommandExample("Update tags on a resource group", "topaz group update \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\" \\\n    --tags '{\"env\":\"prod\"}'")]
public sealed class UpdateResourceGroupCommand(Pipeline eventPipeline, ITopazLogger logger) : Command<UpdateResourceGroupCommand.UpdateResourceGroupCommandSettings>
{
    public override int Execute(CommandContext context, UpdateResourceGroupCommandSettings settings)
    {
        logger.LogDebug(nameof(UpdateResourceGroupCommand), nameof(Execute), "Executing {0}.{1}.", nameof(UpdateResourceGroupCommand), nameof(Execute));

        var controlPlane = new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger), SubscriptionControlPlane.New(eventPipeline, logger), logger);

        var request = new UpdateResourceGroupRequest();
        if (!string.IsNullOrEmpty(settings.Tags))
        {
            request = request with { Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.Tags, GlobalSettings.JsonOptions) };
        }

        var operation = controlPlane.Update(
            new SubscriptionIdentifier(Guid.Parse(settings.SubscriptionId)),
            new ResourceGroupIdentifier(settings.Name!),
            request);

        if (operation.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine(operation.ToString());
            return 1;
        }

        AnsiConsole.WriteLine(JsonSerializer.Serialize(operation.Resource, GlobalSettings.JsonOptionsCli));

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UpdateResourceGroupCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
        {
            return ValidationResult.Error("Resource group subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.SubscriptionId, out _))
        {
            return ValidationResult.Error("Resource group subscription ID must be a valid GUID.");
        }

        return string.IsNullOrEmpty(settings.Name)
            ? ValidationResult.Error("Resource group name can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string Name { get; set; } = null!;

        [CommandOptionDefinition("Tags as a JSON object, e.g. '{\"env\":\"prod\"}'. Replaces existing tags.", required: false)]
        [CommandOption("--tags")]
        public string? Tags { get; set; }
    }
}
