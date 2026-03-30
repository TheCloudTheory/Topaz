# Copilot / AI Agent Instructions for Topaz

This file captures the project-specific knowledge an AI coding agent needs to be productive in Topaz.

Quick start
- Build: `dotnet build Topaz.sln` (there's a VS Code task named `dotnet: build`).
- Tests: `dotnet test Topaz.sln` (tests live under `Topaz.Tests`).
- Run locally: `dotnet run --project Topaz.CLI -- start` (see `Topaz.CLI/Commands/StartCommand.cs`).

Big picture (high level)
- Topaz is a single .NET solution that emulates many Azure services. The host process is `Topaz.Host` which composes services and exposes HTTP/AMQP endpoints.
- Services are implemented under `Topaz.Service.*` (e.g., KeyVault, ServiceBus, EventHub). Each service typically exposes:
  - a control plane (e.g. `*ServiceControlPlane` types)
  - endpoints under `*/Endpoints/*`
  - models under `*/Models/*`
- Resource models implement `ArmResource<T>` and concrete `*ResourceProperties` types.

Important conventions & patterns
- Resource model base: see [Topaz.ResourceManager/ArmResource.cs](Topaz.ResourceManager/ArmResource.cs#L1-L43). Resource IDs follow ARM-like segments; code often parses segments by index (GetSubscription/GetResourceGroup). Do not change the ID format without adjusting these utilities.
- JSON: use project-wide serializer options from [Topaz.Shared/GlobalSettings.cs](Topaz.Shared/GlobalSettings.cs#L1-L44). Use `GlobalSettings.JsonOptions` for endpoint serialization and `JsonOptionsCli` for CLI output.
- Naming: resource model classes end with `Resource` / `ResourceProperties` (e.g., [Services/Topaz.Service.KeyVault/Models/KeyVaultResourceProperties.cs](Services/Topaz.Service.KeyVault/Models/KeyVaultResourceProperties.cs#L1-L84)). Use `FromRequest(...)` or `UpdateFromRequest(...)` patterns when converting API requests to internal models.
- Control vs Data plane: control-plane classes expose CRUD operations and resource listing (`*ServiceControlPlane`). Data-plane classes provide runtime behaviour (e.g., `BlobServiceDataPlane`). Look at `Topaz.Service.Storage` for concrete examples.
- Endpoints & routing: `Topaz.Host` builds a router that matches incoming requests to `IEndpointDefinition` implementations defined by services; services register endpoints via `IServiceDefinition.Endpoints`.
- **One endpoint file per HTTP operation**: Each distinct HTTP operation must live in its own file (e.g., `CreateOrUpdateContainerRegistryEndpoint.cs`, `GetContainerRegistryEndpoint.cs`, `DeleteContainerRegistryEndpoint.cs`). Do **not** combine multiple operations into a single `IEndpointDefinition` class. See `Topaz.Service.Authorization/Endpoints/` and `Topaz.Service.ContainerRegistry/Endpoints/` for canonical examples. Each endpoint class has a single-entry `string[] Endpoints` array, its own `string[] Permissions`, and sets `response.Content.Headers.ContentType` at the end of `GetResponse`.
- Logging & IDs: logger is injected across services; correlation IDs are generated per request in the host (`CorrelationIdFactory`).

Build, run and env notes
- Ports and emulator directory: defaults are in `Topaz.Shared/GlobalSettings.cs` (e.g., `MainEmulatorDirectory = .topaz`). The host will create `.topaz` and `global-dns.json` on first run.
- TLS and certificates: host expects PEM files `topaz.crt`/`topaz.key` or accepts `--certificate-file`/`--certificate-key` CLI options (`Topaz.CLI/Commands/StartCommand.cs`).
- Containerization: `Topaz.CLI/Dockerfile` and `Topaz.MCP/Dockerfile` exist; CI scripts and `publish/` contain packaging helpers. See `scripts/` and `install/` for platform-specific helpers.

Code generation & edits: practical guidelines
- Prefer adding small, focused changes. Keep public APIs and file names consistent with existing `Topaz.Service.*` naming.
- When adding resources, implement `*ResourceProperties` for the contract, a `*Resource` class inheriting `ArmResource<T>`, a `*ResourceProvider` and a `*ServiceControlPlane` following existing services (see `Topaz.Service.KeyVault` or `Topaz.Service.ServiceBus`).
- **`Deploy()` is mandatory**: Every `IControlPlane` implementation must have a working `Deploy()` method (not `throw new NotImplementedException()`). Follow the KeyVault pattern: cast `GenericResource` with `resource.As<TResource, TProperties>()`, map all fields into the create/update request, delegate to `CreateOrUpdate`, and wrap exceptions with `logger.LogError`. After implementing `Deploy()`, also register the new resource type in `TemplateDeploymentOrchestrator.RouteDeployment()` (add a `case "Microsoft.X/y":` entry) and add the service's project as a `<ProjectReference>` in `Topaz.Service.ResourceManager.csproj`.
- Serialization: always use `GlobalSettings.JsonOptions` when serializing/deserializing HTTP request bodies/responses.
- Id handling: if you modify `Id` format, update `ArmResource.GetSubscription()` and `GetResourceGroup()` usages.
- Patterns to copy: `FromRequest(...)` factory methods and `UpdateFromRequest(...)` mutators are common; mirror the null-checks and GetValueOrDefault() idioms used in `KeyVaultResourceProperties.FromRequest`.

Where to look first (recommended reading order)
- [Topaz.Host/Host.cs](Topaz.Host/Host.cs#L1-L260) — host composition, service list, endpoint wiring.
- [Topaz.CLI/Program.cs](Topaz.CLI/Program.cs#L1-L133) and [Topaz.CLI/Commands/StartCommand.cs](Topaz.CLI/Commands/StartCommand.cs#L1-L83) — how commands bootstrap the host.
- [Topaz.ResourceManager/ArmResource.cs](Topaz.ResourceManager/ArmResource.cs#L1-L43) — resource model base and ID parsing.
- [Topaz.Shared/GlobalSettings.cs](Topaz.Shared/GlobalSettings.cs#L1-L44) — JSON and default ports.
- Example service: [Services/Topaz.Service.KeyVault](Services/Topaz.Service.KeyVault/Models/KeyVaultResourceProperties.cs#L1-L84) and its endpoints/control plane.

Tests & CI
- Unit and integration tests live under `Topaz.Tests`. CI workflows run build and test; the repo uses `Nerdbank.GitVersioning` (see `Directory.Build.props`).

When to ask the user
- If a change touches networking ports, resource ID formats, or global serializer options, confirm desired behaviour before applying broad changes.

API Coverage docs (mandatory)
- The `website/docs/api-coverage/` directory contains one Markdown file per service. Each file tracks which Azure REST API operations are implemented in Topaz, mapped to the official Microsoft REST API reference.
- **Always consult** the relevant `api-coverage/<service>.md` file before adding or removing endpoints for a service so you know what is already tracked.
- **Always update** the relevant `api-coverage/<service>.md` file after adding or removing endpoint implementations: flip ❌ → ✅ (or vice-versa) for the affected operations. If the service page is still a stub, fill in the full operation table (use the Azure REST API reference link in the file header as a guide).
- The [Container Registry coverage](website/docs/api-coverage/container-registry.md) page is the canonical example of the completed format.

Mandatory steps
- Always present a summary of changes before applying them, especially for public API changes or anything affecting resource IDs or serialization.
- If you add new services or endpoints, ensure they are registered in the host and have corresponding tests.

If anything is missing or unclear, tell me what area you'd like expanded (build, adding services, routing, testing, or an example change), and I'll iterate.
