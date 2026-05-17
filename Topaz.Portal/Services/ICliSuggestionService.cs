using Topaz.Portal.Models.Cli;

namespace Topaz.Portal.Services;

public interface ICliSuggestionService
{
    IReadOnlyList<CliCommandModel> GetAll();
    IReadOnlyList<CliCommandModel> GetSuggestions(string prefix);
    CliCommandModel? GetCommand(string name);
}
