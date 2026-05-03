using JetBrains.Annotations;
using Topaz.Documentation.Command;
using Topaz.Shared;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription create", "subscription", "Creates a new subscription.")]
[CommandExample("Create a subscription", "topaz subscription create \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\" \\\n    --name \"my-subscription\"" )]
public sealed class CreateSubscriptionCommand(Pipeline eventPipeline, ITopazLogger logger)
    : Command<CreateSubscriptionCommand.CreateSubscriptionCommandSettings>
{
    public override int Execute(CommandContext context, CreateSubscriptionCommandSettings settings)
    {
        AnsiConsole.WriteLine("Creating subscription...");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.Id);
        var controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
        var sa = controlPlane.Create(subscriptionIdentifier, settings.Name!, settings.Tags);

        AnsiConsole.WriteLine(sa.ToString());

        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateSubscriptionCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Id))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (string.IsNullOrEmpty(settings.Name))
        {
            return ValidationResult.Error("Subscription name can't be null.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")] public string? Id { get; set; }

        [CommandOptionDefinition("(Required) Subscription display name.", required: true)]
        [CommandOption("-n|--name")] public string? Name { get; set; }

        [CommandOptionDefinition("Tags to assign to the subscription (key=value).")]
        [CommandOption("-t|--tag")] public IDictionary<string, string>? Tags { get; set; }
    }
}