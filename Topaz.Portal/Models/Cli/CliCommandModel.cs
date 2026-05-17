using System.Text.Json.Serialization;

namespace Topaz.Portal.Models.Cli;

public sealed record CliCommandModel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("options")] CliOptionModel[] Options,
    [property: JsonPropertyName("examples")] CliExampleModel[] Examples
);

public sealed record CliOptionModel(
    [property: JsonPropertyName("names")] string Names,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("required")] bool Required
);

public sealed record CliExampleModel(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("command")] string Command
);

public sealed record CliExecutionResult(string Output, bool IsError);
