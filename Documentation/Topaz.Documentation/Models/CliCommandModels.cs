namespace Topaz.Documentation.Models;

public sealed record CliCommandModel(
    string Name,
    string Group,
    string Description,
    CliOptionModel[] Options,
    CliExampleModel[] Examples
);

public sealed record CliOptionModel(
    string Names,
    string Description,
    bool Required
);

public sealed record CliExampleModel(
    string Title,
    string Command
);
