using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription list-locations", "subscription", "Lists all available Azure locations for a subscription.")]
[CommandExample("List subscription locations", "topaz subscription list-locations \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class ListLocationsCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<ListLocationsCommand.ListLocationsCommandSettings>
{
    public override int Execute(CommandContext context, ListLocationsCommandSettings settings)
    {
        AnsiConsole.WriteLine("Listing locations for subscription...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.Id);
        var controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
        var result = controlPlane.ListLocations(subscriptionIdentifier);

        if (result.Result == OperationResult.NotFound)
        {
            Console.Error.WriteLine($"Subscription '{settings.Id}' not found.");
            return 1;
        }

        var response = new ListLocationsResponse(settings.Id!);
        AnsiConsole.WriteLine(JsonSerializer.Serialize(response, GlobalSettings.JsonOptionsCli));

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListLocationsCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Id))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.Id, out _))
        {
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListLocationsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")] public string? Id { get; set; }
    }
}
