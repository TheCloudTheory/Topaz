---
slug: cli-host-split
title: "How we split Topaz CLI from the Host — and why it matters for scripting"
authors: kamilmrzyglod
tags: [general]
---

Until recently, `topaz start` was the only way to run the emulator. That single command lived inside the CLI, started the Host, and stayed running in your terminal. Useful for getting going quickly. Less useful once you want to script around it, automate it in CI, or distribute the two halves independently.

This post walks through why the split was done, how the boundary between Host and CLI works in practice, and what the health-check endpoint makes possible from any language.

{/* truncate */}

## The original design

The original Topaz CLI was a single executable built on [Spectre.Console.Cli](https://spectreconsole.net/cli/). It loaded every command — resource management, subscriptions, Key Vault, Service Bus, Container Registry — alongside a `start` command that bootstrapped the Host process:

```csharp
// Topaz.CLI/Commands/StartCommand.cs (removed)
public sealed class StartCommand(ITopazLogger logger) : AsyncCommand<...>
{
    public override async Task<int> ExecuteAsync(...)
    {
        var host = new Topaz.Host.Host(new GlobalOptions { ... }, logger);
        await host.StartAsync(Program.CancellationToken);
    }
}
```

`topaz start` held the process open until you hit Ctrl+C, and every other `topaz` sub-command (`topaz keyvault list`, `topaz servicebus create-namespace`, etc.) ran as short-lived processes against the same resource files on disk.

The coupling was pragmatic but created a few problems:

- The CLI binary pulled in the Host assembly and every service project it referenced. To install the management CLI, you got the entire emulator process too.
- In Docker, you could only run a single container image that did both jobs. Separating the long-running emulator from short-lived management calls was not straightforward.
- Automation scripts that needed to start the emulator and then issue commands had to run two commands against the same binary — one blocking, one not — adding shell gymnastics to every CI script.

## The split

The refactor introduced a second binary: `topaz-host`. It has one job — start the emulator and keep it running:

```csharp
// Topaz.Host/Program.cs
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var app = new CommandApp<StartHostCommand>();
        app.Configure(config => config.SetApplicationName("topaz-host"));
        return await app.RunAsync(args);
    }
}
```

`StartHostCommand` takes the same flags that `StartCommand` did (`--log-level`, `--default-subscription`, `--certificate-file`, `--emulator-ip-address`) but lives in `Topaz.Host` rather than `Topaz.CLI`. The CLI project no longer has a dependency on the Host assembly, so neither binary carries the other's weight.

The two binaries are also published as independent Docker images: `thecloudtheory/topaz-host` for the emulator, and `thecloudtheory/topaz` for the CLI.

## The health-check contract

Splitting into two processes immediately raises a co-ordination question: how does the CLI know the Host is running, and that it is the right Host to talk to?

Topaz answers this with a health endpoint that the Host exposes on the Resource Manager port:

```csharp
// Topaz.Host/GetHealthEndpoint.cs
internal sealed class GetHealthEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /health"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new HealthResponse(Environment.CurrentDirectory));
    }

    private sealed record HealthResponse(string WorkingDirectory)
    {
        public string Status => "Healthy";
        // ...
    }
}
```

The response includes `Status` and `WorkingDirectory`. The working directory is the key field. Before executing any command, the CLI calls this endpoint and compares the Host's working directory against its own:

```csharp
// Topaz.CLI/Program.cs
private static async Task<int> CheckHostAsync()
{
    var response = await client.GetAsync(
        $"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/health");
    var json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);

    var hostDir = Path.GetFullPath(wdElement.GetString() ?? string.Empty);
    var cliDir = Path.GetFullPath(Environment.CurrentDirectory);

    if (!string.Equals(hostDir, cliDir, StringComparison.OrdinalIgnoreCase))
    {
        await Console.Error.WriteLineAsync(
            $"Topaz Host is running from a different directory ('{hostDir}'). " +
            "Run the CLI from the same directory as the Host.");
        return 1;
    }

    return 0;
}
```

If the Host is not reachable: clear error, non-zero exit code. If the working directories differ — meaning you have a Host running from `/projects/app-a` and you're trying to issue CLI commands from `/projects/app-b` — another clear error. No silent mismatch.

This pre-flight check runs before any command executes, so the failure mode is always obvious rather than appearing as a confusing resource-not-found deep in a long operation.

## What this unlocks for scripting

The health endpoint responds to any HTTP client. You do not need the Topaz CLI to check whether the emulator is ready — you can use `curl`, `wget`, PowerShell's `Invoke-WebRequest`, or any language's HTTP library:

```bash
# Wait until Topaz is ready, then run tests
until curl -sk https://topaz.local.dev:8899/health | grep -q '"Status":"Healthy"'; do
  sleep 1
done
dotnet test
```

In GitHub Actions, this is how the CI pipeline gates the test step:

```yaml
- name: Wait for Topaz
  run: |
    for i in $(seq 30); do
      curl -sk https://topaz.local.dev:8899/health && break || sleep 2
    done
```

Because `topaz-host` is now a standalone process, it can also run as a proper sidecar in Docker Compose alongside your application, with a healthcheck declaration that other services depend on:

```yaml
services:
  topaz:
    image: thecloudtheory/topaz-host:latest
    ports:
      - "8899:8899"
    healthcheck:
      test: ["CMD", "curl", "-sk", "https://localhost:8899/health"]
      interval: 5s
      retries: 10
  app:
    build: .
    depends_on:
      topaz:
        condition: service_healthy
```

## The two-terminal workflow

In day-to-day development the workflow is:

```
Terminal 1                          Terminal 2
──────────────────────────────      ──────────────────────────────
$ topaz-host --log-level Info       $ topaz keyvault list \
                                        --subscription 00000000-...
                                        --resource-group rg-dev
```

`topaz-host` stays running in the background (or as a background process, or a Docker container). The CLI is stateless — each invocation does its pre-flight check, runs the command, and exits.

The split also means you can keep the Host running across many CLI invocations without any concern about process lifecycle. `topaz-host` owns the emulator state; `topaz` is just the management surface.

## Takeaway

Splitting the CLI from the Host was a small refactor in terms of lines changed, but it clarified the design significantly: one process owns the emulator, another issues commands to it, and a lightweight HTTP contract (the health endpoint) provides the coordination layer. Any tool that can make an HTTP request can participate in that contract — which makes Topaz composable in environments far beyond what a single CLI binary could reach.
