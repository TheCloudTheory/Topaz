# CLAUDE.md — Topaz

Project-specific knowledge for AI coding agents working in this repository.

## Quick commands

```bash
dotnet build Topaz.sln
dotnet test Topaz.sln
dotnet run --project Topaz.Host   # starts the emulator host
dotnet run --project Topaz.CLI    # interacts with a running host
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
| MCP server | `Topaz.MCP/` |

## Where to look first

- [Topaz.Host/Host.cs](Topaz.Host/Host.cs) — host composition, service list, endpoint wiring.
- [Topaz.MCP/](Topaz.MCP/) — MCP server exposing Topaz management tools to AI assistants (GitHub Copilot, Claude, etc.).
- [Topaz.CLI/Program.cs](Topaz.CLI/Program.cs) and [Topaz.CLI/Commands/StartCommand.cs](Topaz.CLI/Commands/StartCommand.cs) — how commands bootstrap the host.
- [Topaz.ResourceManager/ArmResource.cs](Topaz.ResourceManager/ArmResource.cs) — resource model base and ID parsing.
- [Topaz.Shared/GlobalSettings.cs](Topaz.Shared/GlobalSettings.cs) — JSON and default ports.
- Example service: [Services/Topaz.Service.KeyVault/](Services/Topaz.Service.KeyVault/) and its endpoints/control plane.

## Build, run and env notes

- Ports and emulator directory: defaults are in `Topaz.Shared/GlobalSettings.cs` (e.g., `MainEmulatorDirectory = .topaz`). The host will create `.topaz` and `global-dns.json` on first run.
- TLS and certificates: host expects PEM files `topaz.crt`/`topaz.key` or accepts `--certificate-file`/`--certificate-key` CLI options (`Topaz.CLI/Commands/StartCommand.cs`).
- Containerization: `Topaz.CLI/Dockerfile` and `Topaz.MCP/Dockerfile` exist; CI scripts and `publish/` contain packaging helpers. See `scripts/` and `install/` for platform-specific helpers.

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
4. Add new tool in `Topaz.MCP` project. Follow the conventions found in `Tools` folder.
5. Add Terraform and PowerShell tests in `Topaz.Tests.Terraform` and `Topaz.Tests.AzurePowerShell`

### Backlog / Roadmap

- New work → add `<!-- TODO: ... -->` to `BACKLOG.md` **and** a row to `website/src/pages/roadmap.md`.
- Completed work → remove the task from backlog so it will be removed from GitHub issues.
- Badges: `<span class="badge--stable">Stable</span>` or `<span class="badge--preview">Preview</span>` — use CSS classes, never inline styles.
- Badge CSS lives in `website/src/css/custom.css` (`.badge--stable` and `.badge--preview`). Do not add inline styles; always use these classes.
- For simple tasks (CRUD, scafollding, boilerplate) always add `good first issue` label.

## API Coverage docs

- The `website/docs/api-coverage/` directory contains one Markdown file per service. Each file tracks which Azure REST API operations are implemented in Topaz, mapped to the official Microsoft REST API reference.
- **Always consult** the relevant `api-coverage/<service>.md` file before adding or removing endpoints for a service so you know what is already tracked.
- **Always update** the relevant `api-coverage/<service>.md` file after adding or removing endpoint implementations: flip ❌ → ✅ (or vice-versa) for the affected operations. If the service page is still a stub, fill in the full operation table (use the Azure REST API reference link in the file header as a guide).
- The [Container Registry coverage](website/docs/api-coverage/container-registry.md) page is the canonical example of the completed format.

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

### Control / data plane responsibility rule

Control and data planes are the layer between endpoints and resource providers. Endpoints **must never** implement service-specific logic.

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
| `GlobalSettings.DefaultQueueStoragePort` | 8893 |

### Resource model naming

- Classes: `*Resource` / `*ResourceProperties`
- Use `FromRequest(...)` factory methods and `UpdateFromRequest(...)` mutators; mirror null-checks and `GetValueOrDefault()` idioms from `KeyVaultResourceProperties`.
- **All object construction belongs in the model.** Resource type strings (e.g. `"Microsoft.Management/managementGroups"`), ID formatting, and property wiring must live in static factory methods on the model class (e.g. `DescendantInfo.FromManagementGroup(...)`, `DescendantInfo.FromSubscription(...)`). Endpoints and control planes must never build model objects inline — call the factory method instead.

## Tests

Both suites are required for every endpoint or control-plane change.

### `Topaz.Tests/E2E/`
- Use Azure SDK (`ArmClient`, service-specific clients) against the in-process host started by `E2EFixture`.
- One `[Test]` per operation; name: `<Resource>_<Operation>_<ExpectedOutcome>`.

### `Topaz.Tests.AzureCLI/`
- Use `RunAzureCliCommand("az ...")` via `TopazFixture`.
- Use `GlobalSettings.ContainerRegistryPort` directly for port references.
- **DNS**: every new hostname used in a test (registry login server, vault URL, etc.) must be added as a `WithExtraHost(...)` entry in `TopazFixture.cs`. A missing entry causes a silent curl timeout (exit code 28), not a DNS error.
- **Storage data-plane commands** (`az storage queue ...`, `az storage blob ...`) must use `--connection-string` rather than separate `--account-name`/`--account-key`/`--queue-endpoint` flags. The Python SDK only extracts the account name from `*.core.windows.net` and localhost URLs; Topaz's custom domain causes a client-side `ValueError: Unable to determine account name` before any network call is made. Connection string format: `"DefaultEndpointsProtocol=https;AccountName={account};AccountKey={key};QueueEndpoint=https://{account}.queue.storage.topaz.local.dev:8893;"`
- **Rebuild before running**: `Topaz.Tests.AzureCLI` runs against the Docker image, not local binaries. Always rebuild with `./scripts/build-docker.sh arm64` (or `amd64`) after any code change before running these tests. Results from a stale image are not valid evidence.

### Terraform/debugging workflow
- Terraform tests in `Topaz.Tests.Terraform` run against the Docker image (`topaz/host`), not local binaries.
- After every code change that should affect Terraform tests, rebuild first: `./scripts/build-docker.sh arm64` (or `amd64`), then run the filtered test.
- If a build fails, do not trust subsequent Terraform test output as validation of code changes.

### Debugging failing tests — mandatory process

**Before reasoning about a test failure, always:**
1. Run the test locally: `dotnet test <project>.csproj --filter "<TestName>" --logger "console;verbosity=detailed"`
2. Read the Topaz host logs emitted to the test console — the router, endpoint selection, request body, and response body are all logged at Debug/Information level.
3. Only form hypotheses after seeing the actual log output.

**Checking the Docker image timestamp:** verify `docker images topaz/host` shows a build time *after* your last file edit. If not, rebuild.

**Adding `--debug` to an `az` command** (inside a `RunAzureCliCommand`) prints the full HTTP request and response including headers — useful when the SDK appears to ignore a valid Topaz response.

**Terraform CI failure diagnosis:** The CI log only shows the last *successful* HTTP requests before the exit-code-1 failure. Identify what resource is being created (e.g. `azurerm_storage_table_entity`), enumerate the API calls the Terraform provider makes (create + read-back), and check if ALL those endpoint patterns are implemented. Missing GET-by-key endpoints are a common cause (e.g. `GET /{tableName}(PartitionKey='…',RowKey='…')` vs. generic `GET /{tableName}`).

**Table Storage endpoint ordering:** Regex key-based routes (e.g. `GET /^.*?\(PartitionKey=…\)$`) must be registered BEFORE the wildcard `GET /{tableName}` route in `TableStorageService.Endpoints`. Otherwise the wildcard matches first and returns 404 for key lookups.

**Storage data plane account resolution:** `TryGetStorageAccount` in `TableDataPlaneEndpointBase` and `BlobDataPlaneEndpointBase` resolves the account name from the Host subdomain first, then falls back to the `Authorization: SharedKeyLite/SharedKey accountname:...` header. This is needed when Azure CLI uses a plain `--table-endpoint`/`--blob-endpoint` URL instead of the account-specific subdomain URL.

### Investigating Azure CLI / SDK response-parsing issues

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

### `Topaz.Tests.Portal/` (Portal work definition of done)
- Inherit from `BunitTestContext`.
- Register a fake `ITopazClient` via NSubstitute: `Services.AddSingleton(Substitute.For<ITopazClient>())`.
- After a click causes a re-render, re-query with a fresh `cut.Find(...)` — stored references hold stale event-handler IDs.
- Use `cut.WaitForAssertion(...)` for async state changes.
- One `[Test]` per user-visible behaviour; name: `<Component>_<Behaviour>_<ExpectedOutcome>`.

## Azure Queue Storage — response contracts

These differ from intuition; getting them wrong causes `NullReferenceException` inside the Azure SDK error parser.

| Operation | Method | Path | Status | Response |
|---|---|---|---|---|
| Get Queue Metadata | `GET` | `/{queue}?comp=metadata` | 200 | Empty body; `x-ms-approximate-messages-count` header |
| Update Message | `PUT` | `/{queue}/messages/{id}` | **204** | Empty body; `x-ms-popreceipt` + `x-ms-time-next-visible` headers |
| Send Message | `POST` | `/{queue}/messages` | 201 | XML body (`QueueMessagesList`) |

The `UpdateMessage` endpoint returns metadata in **response headers, not the body**. Any non-204 response causes the SDK to throw `RequestFailedException`, which then crashes in `StorageRequestFailedDetailsParser.TryParse` when the body isn't a valid error XML.

## ACR data-plane — implemented endpoints

- `GET /PUT /DELETE /HEAD /v2/{name}/manifests/{reference}`
- `HEAD /GET /v2/{name}/blobs/{digest}`
- `POST /PATCH /PUT` blob uploads
- `GET /v2/_catalog`, `GET /v2/{name}/tags/list`, `GET /acr/v1/{name}/_tags`

## When to check with the user

- Changes to networking ports, resource ID formats, or `GlobalSettings.JsonOptions` — confirm desired behaviour before applying broad changes.
- Public API changes or anything affecting resource IDs or serialization — present a summary before applying.
- New concepts which are not yet introduced

## Naming conventions

- **Always** use full names, do not use abbrevations.

## Git rules

- **Never commit automatically.** Always show the user the proposed commit message and wait for explicit approval before running `git commit`.
