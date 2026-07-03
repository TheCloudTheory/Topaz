using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ResourceManager.Commands;

[UsedImplicitly]
[CommandDefinition("deployment group export-template", "deployment", "Exports an ARM template from a resource group.")]
[CommandExample("Export template from a resource group", "topaz deployment group export-template \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
[CommandExample("Export template with parameterization options", "topaz deployment group export-template \\\n    --name \"my-rg\" \\\n    --subscription-id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\" \\\n    --options \"IncludeParameterDefaultValue,IncludeComments\"")]
public sealed class ExportGroupTemplateCommand(HttpClient httpClient)
    : TopazHttpCommand<ExportGroupTemplateCommand.ExportGroupTemplateCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ExportGroupTemplateCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.Name}/exportTemplate";
        var (success, body) = await PostAsync(url, new { resources = new[] { "*" } });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ExportGroupTemplateCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return string.IsNullOrEmpty(settings.Name)
            ? ValidationResult.Error("Resource group name can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ExportGroupTemplateCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-n|--name")]
        public string Name { get; set; } = null!;

        [CommandOptionDefinition("Export options: comma-separated list of IncludeParameterDefaultValue, IncludeComments, SkipResourceNameParameterization, SkipAllParameterization.", required: false)]
        [CommandOption("-o|--options")]
        public string? Options { get; set; }
    }
}
