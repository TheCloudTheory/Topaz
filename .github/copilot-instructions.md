# Copilot / AI Agent Instructions for Topaz

This file captures the project-specific knowledge an AI coding agent needs to be productive in Topaz.

Quick start
- Build: `dotnet build Topaz.sln` (there's a VS Code task named `dotnet: build`).
- Tests: `dotnet test Topaz.sln` (tests live under `Topaz.Tests`).
- Start the host: `dotnet run --project Topaz.Host` — runs all emulated Azure services.
- Interact with a running host: `dotnet run --project Topaz.CLI` — thin CLI client, requires the host to already be running.

Big picture (high level)
- Topaz is a single .NET solution that emulates many Azure services. The host process is `Topaz.Host` which composes services and exposes HTTP/AMQP endpoints.
- Services are implemented under `Topaz.Service.*` (e.g., KeyVault, ServiceBus, EventHub). Each service typically exposes:
  - a control plane (e.g. `*ServiceControlPlane` types)
  - endpoints under `*/Endpoints/*`
  - models under `*/Models/*`
- Resource models implement `ArmResource<T>` and concrete `*ResourceProperties` types.

Key paths

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
| MCP server | `Topaz.MCP/` |

Important conventions & patterns
- Resource model base: see [Topaz.ResourceManager/ArmResource.cs](../Topaz.ResourceManager/ArmResource.cs). Resource IDs follow ARM-like segments; code often parses segments by index (GetSubscription/GetResourceGroup). Do not change the ID format without adjusting these utilities.
- JSON: use project-wide serializer options from [Topaz.Shared/GlobalSettings.cs](../Topaz.Shared/GlobalSettings.cs). Use `GlobalSettings.JsonOptions` for endpoint serialization and `JsonOptionsCli` for CLI output.
- Naming: resource model classes end with `Resource` / `ResourceProperties` (e.g., [Services/Topaz.Service.KeyVault/Models/KeyVaultResourceProperties.cs](../Services/Topaz.Service.KeyVault/Models/KeyVaultResourceProperties.cs)). Use `FromRequest(...)` or `UpdateFromRequest(...)` patterns when converting API requests to internal models.
- Control vs Data plane: control-plane classes expose CRUD operations and resource listing (`*ServiceControlPlane`). Data-plane classes provide runtime behaviour (e.g., `BlobServiceDataPlane`). Look at `Topaz.Service.Storage` for concrete examples.
- Endpoints & routing: `Topaz.Host` builds a router that matches incoming requests to `IEndpointDefinition` implementations defined by services; services register endpoints via `IServiceDefinition.Endpoints`.
- **One endpoint file per HTTP operation**: Each distinct HTTP operation must live in its own file (e.g., `CreateOrUpdateContainerRegistryEndpoint.cs`, `GetContainerRegistryEndpoint.cs`, `DeleteContainerRegistryEndpoint.cs`). Do **not** combine multiple operations into a single `IEndpointDefinition` class. See `Topaz.Service.Authorization/Endpoints/` and `Topaz.Service.ContainerRegistry/Endpoints/` for canonical examples. Each endpoint class has a single-entry `string[] Endpoints` array, its own `string[] Permissions`, and sets `response.Content.Headers.ContentType` at the end of `GetResponse`.
- **Model ARM-managed nested objects as subresources**: If an operation represents an ARM-manageable nested object (e.g., `.../networkRuleSets/{name}`), introduce a persisted `ArmSubresource<T>` model and store/read it via `ResourceProviderBase.CreateOrUpdateSubresource` and `GetSubresourceAs`. Do not return ephemeral hard-coded DTOs from endpoints for such resources.
- Logging & IDs: logger is injected across services; correlation IDs are generated per request in the host (`CorrelationIdFactory`).

Endpoint structure template:

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

HEAD response pattern:

```csharp
response.Headers.Add("Docker-Content-Digest", digest);
response.Content = new ByteArrayContent([]);
response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
response.Content.Headers.ContentLength = size;
response.StatusCode = HttpStatusCode.OK;
```

Ports — never hardcode. Always use `GlobalSettings.*Port` constants:

| Constant | Port |
|---|---|
| `GlobalSettings.ContainerRegistryPort` | 8892 |
| `GlobalSettings.DefaultKeyVaultPort` | 8898 |
| `GlobalSettings.DefaultResourceManagerPort` | 8899 |
| `GlobalSettings.DefaultBlobStoragePort` | 8891 |
| `GlobalSettings.DefaultQueueStoragePort` | 8893 |

Response shaping for AzureRM/Terraform:
- Use `response.CreateJsonContentResponse(...)` for JSON endpoints; do not build JSON responses via `StringContent` directly.
- Any response DTO returned through `CreateJsonContentResponse` must implement `ToString()` with `JsonSerializer.Serialize(this, GlobalSettings.JsonOptions)`.
- Keep status codes aligned with AzureRM expectations for the specific operation. For Event Hub compatibility in current tests: namespace create can be `201`, while Event Hub and Event Hub networkRuleSets PUT handlers should return `200`.

Build, run and env notes
- Ports and emulator directory: defaults are in `Topaz.Shared/GlobalSettings.cs` (e.g., `MainEmulatorDirectory = .topaz`). The host will create `.topaz` and `global-dns.json` on first run.
- TLS and certificates: host expects PEM files `topaz.crt`/`topaz.key` or accepts `--certificate-file`/`--certificate-key` CLI options (`Topaz.CLI/Commands/StartCommand.cs`).
- Containerization: `Topaz.CLI/Dockerfile` and `Topaz.MCP/Dockerfile` exist; CI scripts and `publish/` contain packaging helpers. See `scripts/` and `install/` for platform-specific helpers.

Code generation & edits: practical guidelines
- Prefer adding small, focused changes. Keep public APIs and file names consistent with existing `Topaz.Service.*` naming.
- When adding resources, implement `*ResourceProperties` for the contract, a `*Resource` class inheriting `ArmResource<T>`, a `*ResourceProvider` and a `*ServiceControlPlane` following existing services (see `Topaz.Service.KeyVault` or `Topaz.Service.ServiceBus`).
- **`Deploy()` is mandatory**: Every `IControlPlane` implementation must have a working `Deploy()` method (not `throw new NotImplementedException()`). Follow the KeyVault pattern: cast `GenericResource` with `resource.As<TResource, TProperties>()`, map all fields into the create/update request, delegate to `CreateOrUpdate`, and wrap exceptions with `logger.LogError`. After implementing `Deploy()`, also register the new resource type in `TemplateDeploymentOrchestrator.RouteDeployment()` (add a `case "Microsoft.X/y":` entry) and add the service's project as a `<ProjectReference>` in `Topaz.Service.ResourceManager.csproj`.
- Serialization: always use `GlobalSettings.JsonOptions` when serializing/deserializing HTTP request bodies/responses.
- Endpoint JSON responses: prefer `response.CreateJsonContentResponse(...)`. If using a response DTO with `CreateJsonContentResponse`, ensure the DTO overrides `ToString()` to serialize with `GlobalSettings.JsonOptions`.
- Id handling: if you modify `Id` format, update `ArmResource.GetSubscription()` and `GetResourceGroup()` usages.
- **Filesystem access via resource providers only**: Never access the filesystem directly from a control plane or endpoint class. All reads and writes must go through a `ResourceProviderBase<TService>` subclass (e.g., `FooResourceProvider`). See `ManagedIdentityResourceProvider` and `SystemAssignedIdentityResourceProvider` as examples. Breaking this rule re-introduces direct file I/O scattered across classes and makes persistence non-uniform.
- **Model placement**: All model classes (resource models, request/response DTOs) **must** live in the service's `Models/` directory. Never define models as nested or private classes inside endpoint files.
- **Model construction**: All object construction and default-value logic for a model belongs in `static` factory methods on the model class (e.g. `FromSite(...)`, `FromRequest(...)`). Neither endpoints nor control planes may construct model objects inline.
- Patterns to copy: `FromRequest(...)` factory methods and `UpdateFromRequest(...)` mutators are common; mirror the null-checks and GetValueOrDefault() idioms used in `KeyVaultResourceProperties.FromRequest`.

Where to look first (recommended reading order)
- [Topaz.Host/Host.cs](../Topaz.Host/Host.cs) — host composition, service list, endpoint wiring.
- [Topaz.MCP/](../Topaz.MCP/) — MCP server exposing Topaz management tools to AI assistants (GitHub Copilot, Claude, etc.).
- [Topaz.CLI/Program.cs](../Topaz.CLI/Program.cs) and [Topaz.CLI/Commands/StartCommand.cs](../Topaz.CLI/Commands/StartCommand.cs) — how commands bootstrap the host.
- [Topaz.ResourceManager/ArmResource.cs](../Topaz.ResourceManager/ArmResource.cs) — resource model base and ID parsing.
- [Topaz.Shared/GlobalSettings.cs](../Topaz.Shared/GlobalSettings.cs) — JSON and default ports.
- Example service: [Services/Topaz.Service.KeyVault](../Services/Topaz.Service.KeyVault/Models/KeyVaultResourceProperties.cs) and its endpoints/control plane.

Tests & CI
- Unit and integration tests live under `Topaz.Tests`. CI workflows run build and test; the repo uses `Nerdbank.GitVersioning` (see [Directory.Build.props](../Directory.Build.props)).
- Both test suites are required for every endpoint or control-plane change.

`Topaz.Tests/E2E/`
- Use Azure SDK (`ArmClient`, service-specific clients) against the in-process host started by `E2EFixture`.
- One `[Test]` per operation; name: `<Resource>_<Operation>_<ExpectedOutcome>`.
- Prefer a dedicated `[Test]` method per operation rather than expanding an existing test; reuse known stable built-in resources (e.g. the Reader role `acdd72a7-3385-48ef-bd42-f606fba81ae7`) where no setup/teardown is needed.

`Topaz.Tests.AzureCLI/`
- Use `RunAzureCliCommand("az ...")` via `TopazFixture`.
- Use `GlobalSettings.ContainerRegistryPort` directly for port references.
- **DNS**: every new hostname used in a test (registry login server, vault URL, etc.) must be added as a `WithExtraHost(...)` entry in `TopazFixture.cs`. A missing entry causes a silent curl timeout (exit code 28), not a DNS error.
- **Never hardcode subscription IDs in test URLs** — Topaz generates a fresh subscription ID per test-container run. Use shell substitution instead: `$(az account show --query id -o tsv)` inside the URL string passed to `az rest`. Because `RunAzureCliCommand` executes via `/bin/sh -c`, `$(...)` expansion works inside double-quoted strings, e.g. `"https://topaz.local.dev:8899/subscriptions/$(az account show --query id -o tsv)/providers/..."`. 
- **Storage data-plane commands**
- **Rebuild before running**: `Topaz.Tests.AzureCLI` runs against the Docker image, not local binaries. Always rebuild with `./scripts/build-docker.sh arm64` (or `amd64`) after any code change before running these tests. Results from a stale image are not valid evidence.

Terraform tests
- `Topaz.Tests.Terraform` runs against the Docker image (`topaz/host`), not local binaries.
- After every code change that should affect Terraform tests, rebuild first: `./scripts/build-docker.sh arm64` (or `amd64`), then run the filtered test.
- If a build fails, do not trust subsequent Terraform test output as validation of code changes.

Debugging failing tests — mandatory process

**Before reasoning about a test failure, always:**
1. Run the test locally: `dotnet test <project>.csproj --filter "<TestName>" --logger "console;verbosity=detailed"`
2. Read the Topaz host logs emitted to the test console — the router, endpoint selection, request body, and response body are all logged at Debug/Information level.
3. Only form hypotheses after seeing the actual log output.

**Checking the Docker image timestamp:** verify `docker images topaz/host` shows a build time *after* your last file edit. If not, rebuild.

**Adding `--debug` to an `az` command** (inside a `RunAzureCliCommand`) prints the full HTTP request and response including headers — useful when the SDK appears to ignore a valid Topaz response.

**Terraform CI failure diagnosis:** The CI log only shows the last *successful* HTTP requests before the exit-code-1 failure. Identify what resource is being created (e.g. `azurerm_storage_table_entity`), enumerate the API calls the Terraform provider makes (create + read-back), and check if ALL those endpoint patterns are implemented. Missing GET-by-key endpoints are a common cause (e.g. `GET /{tableName}(PartitionKey='…',RowKey='…')` vs. generic `GET /{tableName}`).

**Table Storage endpoint ordering:** Regex key-based routes (e.g. `GET /^.*?\(PartitionKey=…\)$`) must be registered BEFORE the wildcard `GET /{tableName}` route in `TableStorageService.Endpoints`. Otherwise the wildcard matches first and returns 404 for key lookups.

**Storage data plane account resolution:** `TryGetStorageAccount` in `TableDataPlaneEndpointBase` and `BlobDataPlaneEndpointBase` resolves the account name from the Host subdomain first, then falls back to the `Authorization: SharedKeyLite/SharedKey accountname:...` header. This is needed when Azure CLI uses a plain `--table-endpoint`/`--blob-endpoint` URL instead of the account-specific subdomain URL.

Investigating Azure CLI / SDK response-parsing issues

When an azure-cli command returns unexpected JSON (e.g. missing fields that Topaz clearly returned):

1. **Check the CLI transform first.** Every `az storage ...` command registered in `commands.py` can have a `transform=` kwarg that post-processes the SDK result before printing. The transform may intentionally drop fields. Find it with:
   ```bash
   docker run --rm mcr.microsoft.com/azure-cli:2.84.0 \
     grep -n '<subcommand>' /usr/lib/az/lib/python3.12/site-packages/azure/cli/command_modules/storage/commands.py
   ```
   Then read the referenced transformer in `_transformers.py`.

2. **Simulate the SDK deserialization.** Run a quick Python script inside the CLI container to verify the SDK can parse your XML/JSON:
   ```bash
   docker run --rm mcr.microsoft.com/azure-cli:2.84.0 sh -c "PYTHONPATH=/usr/lib/az/lib/python3.12/site-packages python3 << 'EOF'
   from azure.storage.blob._generated._utils.serialization import Deserializer, RawDeserializer
   from azure.storage.blob._generated import models as _models
   client_models = {k: v for k, v in _models.__dict__.items() if isinstance(v, type)}
   result = RawDeserializer.deserialize_from_http_generics('<your xml>', {'content-type': 'application/xml'})
   print(Deserializer(client_models)('[SignedIdentifier]', result))
   EOF"
   ```

3. **Known SDK behaviour — `content_type` is read from the `content-type` response header.** `_RequestsTransportResponseBase.__init__` sets `self.content_type = requests_response.headers.get("content-type")`. If the header is absent or wrong, XML is parsed as JSON (default) and deserialization silently returns `None` or an empty list.

4. **Known CLI behaviour — `az storage container show-permission`** always strips `signed_identifiers` via `transform_container_permission_output` and only returns `{"publicAccess": "..."}`. To read stored access policies use `az storage container policy list` instead.

5. **ContentDecodePolicy skips deserialization when `stream=True`.** The policy's `on_response` returns early if `response.context.options.get("stream", True)` is truthy. Generated operation code explicitly sets `_stream = False` before calling the pipeline, which is the correct pattern.

6. **`az webapp list` silently drops items where `kind` is absent** — `list_webapp` (azure-cli 2.84.0) filters: `return list(filter(lambda x: x.kind is not None and "function" not in x.kind.lower(), full_list))`. Any web app resource **must** include a non-null `kind` (e.g. `"app"`). Sites created without an explicit kind in the PUT request must default to `"app"` in the control plane.

7. **`az webapp show` crashes when `possibleOutboundIpAddresses` is absent** — `show_app` calls `_remove_list_duplicates(app)` which does `webapp.possible_outbound_ip_addresses.split(',')`. If the field is absent the CLI throws `AttributeError: 'NoneType' object has no attribute 'split'`. Every App Service site resource must include `possibleOutboundIpAddresses` (empty string `""` is valid).

8. **`GenericResourceExpanded.From` must propagate `Kind`** — The shared helper maps `Id`, `Name`, `Type`, `Location`, `Tags`, `Properties` but historically dropped `Kind`. This silently strips `kind` from all generic list responses, breaking CLI filters such as the one in point 6. Always verify that `GenericResourceExpanded` forwards every ARM field when extending it.

`Topaz.Tests.Portal/` (Portal work definition of done)
- Inherit from `BunitTestContext`.
- Register a fake `ITopazClient` via NSubstitute: `Services.AddSingleton(Substitute.For<ITopazClient>())`.
- After a click causes a re-render, re-query with a fresh `cut.Find(...)` — stored references hold stale event-handler IDs.
- Use `cut.WaitForAssertion(...)` for async state changes.
- One `[Test]` per user-visible behaviour; name: `<Component>_<Behaviour>_<ExpectedOutcome>`.

When to ask the user
- If a change touches networking ports, resource ID formats, or global serializer options, confirm desired behaviour before applying broad changes.

API Coverage docs (mandatory)
- The `website/docs/api-coverage/` directory contains one Markdown file per service. Each file tracks which Azure REST API operations are implemented in Topaz, mapped to the official Microsoft REST API reference.
- **Always consult** the relevant `api-coverage/<service>.md` file before adding or removing endpoints for a service so you know what is already tracked.
- **Always update** the relevant `api-coverage/<service>.md` file after adding or removing endpoint implementations: flip ❌ → ✅ (or vice-versa) for the affected operations. If the service page is still a stub, fill in the full operation table (use the Azure REST API reference link in the file header as a guide).
- The [Container Registry coverage](../website/docs/api-coverage/container-registry.md) page is the canonical example of the completed format.

Backlog & Roadmap (mandatory)
- `BACKLOG.md` (repo root) is the single source of truth for planned work. It contains `<!-- TODO: ... -->` blocks that the CI action converts to GitHub Issues automatically.
- `website/src/pages/roadmap.md` is the public-facing view of the same plan, rendered as tables with `<span class="badge--stable">Stable</span>` or `<span class="badge--preview">Preview</span>` badges in a **Status** column.
- **When adding items to `BACKLOG.md`**: always mirror them in `website/src/pages/roadmap.md` — add a new row to the matching milestone table (or create the milestone section if it doesn't exist yet). Choose the badge based on the expected quality at release: use `Stable` for well-defined, fully-specified features; use `Preview` for exploratory or incomplete implementations.
- **When completing items**: leave the row as-is — never delete, modify, or strike through roadmap rows. Never delete rows from the roadmap.
- Badge CSS lives in `website/src/css/custom.css` (`.badge--stable` and `.badge--preview`). Do not add inline styles; always use these classes.

Mandatory steps
- Always present a summary of changes before applying them, especially for public API changes or anything affecting resource IDs or serialization.
- If you add new services or endpoints, ensure they are registered in the host and have corresponding tests.
- **Always add or update tests** when implementing or modifying endpoints / control-plane logic. Both test suites must be covered:
  - `Topaz.Tests/E2E/` — use the Azure SDK (`ArmClient`, service-specific clients) against the in-process Topaz host started by `E2EFixture`.
  - `Topaz.Tests.AzureCLI/` — use `RunAzureCliCommand(...)` with an `az ...` command string and an optional assertion callback. Tests run against a Dockerised Topaz + Azure CLI container pair via `TopazFixture`. Every new hostname (registry login server, vault URL, etc.) must be registered as a `WithExtraHost(...)` entry in `TopazFixture.cs` — a missing entry causes a silent curl timeout (exit code 28), not a DNS error.
  - Prefer a dedicated `[Test]` method per operation rather than expanding an existing test; reuse known stable built-in resources (e.g. the Reader role `acdd72a7-3385-48ef-bd42-f606fba81ae7`) where no setup/teardown is needed.
- **Terraform test rule**: `Topaz.Tests.Terraform` uses Dockerized Topaz. After any code change expected to affect Terraform behavior, rebuild the image (`./scripts/build-docker.sh <arch>`) before running Terraform tests. Treat test runs without a rebuild as non-authoritative.
- **Portal work (definition of done)**: Any task that adds or modifies `Topaz.Portal` UI behaviour **must** include a bUnit component test in `Topaz.Tests.Portal/`. Key conventions:
  - Inherit from `BunitTestContext` (not `Bunit.TestContext` or NUnit's `TestContext` directly).
  - Register a fake `ITopazClient` via NSubstitute: `Services.AddSingleton(Substitute.For<ITopazClient>())`.
  - When a click causes a re-render, always re-query elements with a fresh `cut.Find(...)` before calling `.Change()` — stored references hold stale event-handler IDs and will throw.
  - Use `cut.WaitForAssertion(...)` for async state changes.
  - One `[Test]` method per user-visible behaviour; name it `<Component>_<Behaviour>_<ExpectedOutcome>`.
Azure Queue Storage — response contracts

These differ from intuition; getting them wrong causes `NullReferenceException` inside the Azure SDK error parser.

| Operation | Method | Path | Status | Response |
|---|---|---|---|---|
| Get Queue Metadata | `GET` | `/{queue}?comp=metadata` | 200 | Empty body; `x-ms-approximate-messages-count` header |
| Update Message | `PUT` | `/{queue}/messages/{id}` | **204** | Empty body; `x-ms-popreceipt` + `x-ms-time-next-visible` headers |
| Send Message | `POST` | `/{queue}/messages` | 201 | XML body (`QueueMessagesList`) |

The `UpdateMessage` endpoint returns metadata in **response headers, not the body**. Any non-204 response causes the SDK to throw `RequestFailedException`, which then crashes in `StorageRequestFailedDetailsParser.TryParse` when the body isn't a valid error XML.

ACR data-plane — implemented endpoints
- `GET /PUT /DELETE /HEAD /v2/{name}/manifests/{reference}`
- `HEAD /GET /v2/{name}/blobs/{digest}`
- `POST /PATCH /PUT` blob uploads
- `GET /v2/_catalog`, `GET /v2/{name}/tags/list`, `GET /acr/v1/{name}/_tags`

Git rules
- **Never commit automatically.** Always show the user the proposed commit message and wait for explicit approval before running `git commit`.
