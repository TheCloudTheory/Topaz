# Contributing to Topaz

Thank you for your interest in contributing. This document covers how to get started, what to expect from the review process, and the licensing terms that apply to contributions.

## Licensing

Topaz is dual-licensed. The open-source edition is available under the [Apache License 2.0](LICENSE). A commercial license for enterprise use is in preparation.

**Because of the dual-license model, all contributors must sign a Contributor License Agreement (CLA) before their pull request can be merged.**

The CLA grants TheCloudTheory the right to include your contribution in both the open-source and commercial editions of Topaz. You retain copyright over your contribution — the CLA does not transfer ownership. If you are contributing on behalf of an employer, you may need your employer's approval before signing.

> The CLA process is currently being set up. Until it is in place, open your PR and a maintainer will follow up with you directly. Do not let this stop you from contributing.

## Getting started

**Prerequisites:** .NET 8 SDK, Docker (for integration tests).

```bash
git clone https://github.com/TheCloudTheory/Topaz
cd Topaz
dotnet build Topaz.sln
dotnet test Topaz.sln
```

To run Topaz locally:

```bash
dotnet run --project Topaz.CLI -- start
```

## What to work on

Check the [open issues](https://github.com/TheCloudTheory/Topaz/issues) — issues labelled `good first issue` are a good starting point. For larger changes (new services, breaking behaviour changes), open an issue first to align on the approach before writing code.

The [API coverage docs](https://topaz.thecloudtheory.com/docs/api-coverage/) show which Azure operations are implemented and which are not. Filling gaps there is always welcome.

## Before submitting a pull request

### Every endpoint change

1. **One file per HTTP operation** in `Endpoints/` — never combine multiple operations in one `IEndpointDefinition`.
2. Register the endpoint in `*Service.cs` → `Endpoints` property.
3. Update `website/docs/api-coverage/<service>.md` — flip ❌ → ✅ for the operation.
4. Add tests in **both** test suites (see below).

### Every new service

1. Implement `Deploy()` — never `throw new NotImplementedException()`. Follow the KeyVault pattern.
2. Register in `TemplateDeploymentOrchestrator.RouteDeployment()` with `case "Microsoft.X/y":`.
3. Add a `<ProjectReference>` in `Topaz.Service.ResourceManager.csproj`.

### Tests — both suites are required

| Suite | Location | How |
|---|---|---|
| Azure SDK (E2E) | `Topaz.Tests/E2E/` | `ArmClient` / service clients against in-process host |
| Azure CLI | `Topaz.Tests.AzureCLI/` | `RunAzureCliCommand("az ...")` via `TopazFixture` |

Naming convention: `<Resource>_<Operation>_<ExpectedOutcome>`.

If you add a new hostname in the AzureCLI suite, add a `WithExtraHost(...)` entry in `TopazFixture.cs` — missing entries cause timeout failures.

### Coding conventions (short version)

- Use `GlobalSettings.JsonOptions` for all HTTP serialization; use `GlobalSettings.*Port` constants — never hardcode port numbers.
- Never access the filesystem directly from a control plane or endpoint; always go through a `ResourceProviderBase<TService>` subclass.
- Use `response.CreateJsonContentResponse(...)` for JSON responses; any response DTO must implement `ToString()` with `JsonSerializer.Serialize(this, GlobalSettings.JsonOptions)`.
- Resource models: `*Resource` / `*ResourceProperties`, with `FromRequest(...)` factory methods and `UpdateFromRequest(...)` mutators.

The full conventions are in [CLAUDE.md](CLAUDE.md).

## Pull request process

1. Fork the repo and create a branch from `main`.
2. Make your changes, including tests and docs updates.
3. Open a pull request with a clear description of what the change does and why.
4. A maintainer will review. Expect feedback within a few days for small changes; larger changes may take longer.
5. Once approved and the CLA is confirmed, the PR will be merged.

## Reporting bugs

Open a [GitHub issue](https://github.com/TheCloudTheory/Topaz/issues). Include the Topaz version, the Azure SDK / CLI version you are using, and a minimal reproduction.

## Questions

Use [GitHub Discussions](https://github.com/TheCloudTheory/Topaz/discussions) for usage questions, ideas, or anything that is not a bug report.
