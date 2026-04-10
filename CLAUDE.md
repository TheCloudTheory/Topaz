# CLAUDE.md — Topaz

Project-specific knowledge for AI coding agents working in this repository.

## Quick commands

```bash
dotnet build Topaz.sln
dotnet test Topaz.sln
dotnet run --project Topaz.CLI -- start
```

## Architecture

Topaz emulates Azure services in a single .NET 8 solution. The host process (`Topaz.Host/Host.cs`) composes services and exposes HTTP/AMQP endpoints.

Services live under `Services/Topaz.Service.*`. Each service has:
- Control plane (`*ServiceControlPlane`) — CRUD, resource listing
- Data plane (e.g. `AcrDataPlane`) — runtime/protocol behaviour
- `Endpoints/` — one file per HTTP operation
- `Models/` — resource models (`ArmResource<T>` + `*ResourceProperties`)

## Key paths

| Purpose | Path |
|---|---|
| Host composition | `Topaz.Host/Host.cs` |
| Shared settings / ports | `Topaz.Shared/GlobalSettings.cs` |
| ARM resource base | `Topaz.ResourceManager/ArmResource.cs` |
| Container Registry service | `Services/Topaz.Service.ContainerRegistry/` |
| E2E tests (Azure SDK) | `Topaz.Tests/E2E/` |
| E2E tests (Azure CLI) | `Topaz.Tests.AzureCLI/` |
| Portal tests | `Topaz.Tests.Portal/` |
| API coverage docs | `website/docs/api-coverage/` |
| Backlog | `BACKLOG.md` (root) + `website/docs/roadmap.md` |

## Mandatory steps

### Every endpoint change

1. **One file per HTTP operation** in `Endpoints/` — never combine multiple operations in one `IEndpointDefinition`.
2. Register in `*Service.cs` → `Endpoints` property.
3. Update `website/docs/api-coverage/<service>.md` — flip ❌ → ✅ for implemented operations.
4. Add tests in **both** suites (see Tests section).
5. For ARM-manageable nested objects (for example network rule sets), model them as `ArmSubresource<T>` and persist them via `CreateOrUpdateSubresource` / `GetSubresourceAs` instead of building ad-hoc response DTOs in endpoints.

### Every new service (control plane)

1. Implement `Deploy()` — never `throw new NotImplementedException()`. Follow the KeyVault pattern: cast `GenericResource` → `resource.As<TResource, TProperties>()`, map fields, delegate to `CreateOrUpdate`.
2. Register in `TemplateDeploymentOrchestrator.RouteDeployment()` with `case "Microsoft.X/y":`.
3. Add `<ProjectReference>` in `Topaz.Service.ResourceManager.csproj`.

### Backlog / Roadmap

- New work → add `<!-- TODO: ... -->` to `BACKLOG.md` **and** a row to `website/docs/roadmap.md`.
- Completed work → remove/strikethrough from both files.
- Badges: `<span class="badge--stable">Stable</span>` or `<span class="badge--preview">Preview</span>` — use CSS classes, never inline styles.

## Coding conventions

### Endpoint structure

```csharp
internal sealed class VerbResourceEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["VERB /v2/{name}/resource/{ref}"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options) { ... }
}
```

- Path params: `context.Request.Path.Value.ExtractValueFromPath(index)` (index 2 = `{name}`, 4 = `{reference}`)
- Always set `response.Content.Headers.ContentType` at the end of `GetResponse`.

### HEAD response pattern

```csharp
response.Headers.Add("Docker-Content-Digest", digest);
response.Content = new ByteArrayContent([]);
response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
response.Content.Headers.ContentLength = size;
response.StatusCode = HttpStatusCode.OK;
```

### Filesystem rule

Never access the filesystem directly from a control plane or endpoint. All reads and writes must go through a `ResourceProviderBase<TService>` subclass.

### Serialization

Always use `GlobalSettings.JsonOptions` for HTTP request/response serialization. Use `JsonOptionsCli` for CLI output.

### Response shaping for AzureRM/Terraform

- Use `response.CreateJsonContentResponse(...)` for JSON endpoints; do not build JSON responses via `StringContent` directly.
- Any response DTO returned through `CreateJsonContentResponse` must implement `ToString()` with `JsonSerializer.Serialize(this, GlobalSettings.JsonOptions)`.
- Keep status codes aligned with AzureRM expectations for the specific operation. For Event Hub compatibility in current tests: namespace create can be `201`, while Event Hub and Event Hub networkRuleSets PUT handlers should return `200`.

### Ports — never hardcode

Always use `GlobalSettings.*Port` constants:

| Constant | Port |
|---|---|
| `GlobalSettings.ContainerRegistryPort` | 8892 |
| `GlobalSettings.DefaultKeyVaultPort` | 8898 |
| `GlobalSettings.DefaultResourceManagerPort` | 8899 |
| `GlobalSettings.DefaultBlobStoragePort` | 8891 |

### Resource model naming

- Classes: `*Resource` / `*ResourceProperties`
- Use `FromRequest(...)` factory methods and `UpdateFromRequest(...)` mutators; mirror null-checks and `GetValueOrDefault()` idioms from `KeyVaultResourceProperties`.

## Tests

Both suites are required for every endpoint or control-plane change.

### `Topaz.Tests/E2E/`
- Use Azure SDK (`ArmClient`, service-specific clients) against the in-process host started by `E2EFixture`.
- One `[Test]` per operation; name: `<Resource>_<Operation>_<ExpectedOutcome>`.

### `Topaz.Tests.AzureCLI/`
- Use `RunAzureCliCommand("az ...")` via `TopazFixture`.
- Use `GlobalSettings.ContainerRegistryPort` directly for port references.

### Terraform/debugging workflow
- Terraform tests in `Topaz.Tests.Terraform` run against the Docker image (`topaz/cli`), not local binaries.
- After every code change that should affect Terraform tests, rebuild first: `./scripts/build-docker.sh arm64` (or `amd64`), then run the filtered test.
- If a build fails, do not trust subsequent Terraform test output as validation of code changes.

### `Topaz.Tests.Portal/` (Portal work definition of done)
- Inherit from `BunitTestContext`.
- Register a fake `ITopazClient` via NSubstitute: `Services.AddSingleton(Substitute.For<ITopazClient>())`.
- After a click causes a re-render, re-query with a fresh `cut.Find(...)` — stored references hold stale event-handler IDs.
- Use `cut.WaitForAssertion(...)` for async state changes.
- One `[Test]` per user-visible behaviour; name: `<Component>_<Behaviour>_<ExpectedOutcome>`.

## ACR data-plane — implemented endpoints

- `GET /PUT /DELETE /HEAD /v2/{name}/manifests/{reference}`
- `HEAD /GET /v2/{name}/blobs/{digest}`
- `POST /PATCH /PUT` blob uploads
- `GET /v2/_catalog`, `GET /v2/{name}/tags/list`, `GET /acr/v1/{name}/_tags`

## When to check with the user

- Changes to networking ports, resource ID formats, or `GlobalSettings.JsonOptions` — confirm desired behaviour before applying broad changes.
- Public API changes or anything affecting resource IDs or serialization — present a summary before applying.
