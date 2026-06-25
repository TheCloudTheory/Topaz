using Spectre.Console.Cli;
using Topaz.Chaos.Commands.Rules;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands;

public sealed class GenericChaosCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("chaos", chaos =>
        {
            chaos.AddCommand<EnableChaosCommand>("enable");
            chaos.AddCommand<DisableChaosCommand>("disable");
            chaos.AddCommand<GetChaosStatusCommand>("status");

            chaos.AddBranch("rule", rule =>
            {
                rule.AddCommand<CreateChaosRuleCommand>("create");
                rule.AddCommand<GetChaosRuleCommand>("show");
                rule.AddCommand<DeleteChaosRuleCommand>("delete");
                rule.AddCommand<ListChaosRulesCommand>("list");
                rule.AddCommand<EnableChaosRuleCommand>("enable");
                rule.AddCommand<DisableChaosRuleCommand>("disable");
            });
        });
    }
}
