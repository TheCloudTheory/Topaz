using System.Text.Json;
using Topaz.Portal.Models.Cli;

namespace Topaz.Portal.Services;

public sealed class CliSuggestionService : ICliSuggestionService
{
    private const int MaxSuggestions = 8;
    private readonly IReadOnlyList<CliCommandModel> _commands;

    public CliSuggestionService(IWebHostEnvironment environment)
    {
        var catalogPath = Path.Combine(environment.WebRootPath, "cli", "commands.json");

        if (!File.Exists(catalogPath))
        {
            _commands = [];
            return;
        }

        var json = File.ReadAllText(catalogPath);
        _commands = JsonSerializer.Deserialize<CliCommandModel[]>(json) ?? [];
    }

    public IReadOnlyList<CliCommandModel> GetAll() => _commands;

    public IReadOnlyList<CliCommandModel> GetSuggestions(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return [];

        var trimmed = prefix.Trim();

        return _commands
            .Where(c => c.Name.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSuggestions)
            .ToList();
    }

    public CliCommandModel? GetCommand(string name) =>
        _commands.FirstOrDefault(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
}
