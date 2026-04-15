## Summary

<!-- What does this PR do and why? One or two sentences. -->

Closes #<!-- issue number -->

## Type of change

- [ ] Bug fix
- [ ] New API endpoint
- [ ] New Azure service
- [ ] Enhancement to existing functionality
- [ ] Documentation / API coverage update only
- [ ] Other (describe below)

---

## Checklist

### Every PR
- [ ] The build passes (`dotnet build Topaz.sln`)
- [ ] Tests pass (`dotnet test Topaz.sln`)
- [ ] No hardcoded ports — `GlobalSettings.*Port` constants used throughout

### Endpoint changes
- [ ] One file per HTTP operation in `Endpoints/` — no combined operations
- [ ] Endpoint registered in the relevant `*Service.cs` → `Endpoints` property
- [ ] API coverage doc updated (`website/docs/api-coverage/<service>.md`) — ❌ flipped to ✅
- [ ] Tests added in `Topaz.Tests/E2E/` (Azure SDK)
- [ ] Tests added in `Topaz.Tests.AzureCLI/` (Azure CLI)

### New service
- [ ] `Deploy()` implemented — no `throw new NotImplementedException()`
- [ ] Registered in `TemplateDeploymentOrchestrator.RouteDeployment()`
- [ ] `<ProjectReference>` added in `Topaz.Service.ResourceManager.csproj`

### Backlog / roadmap
- [ ] New work added to `BACKLOG.md` and `website/docs/roadmap.md`
- [ ] Completed work removed or marked done in both files

### Contributor
- [ ] I have read the [CONTRIBUTING.md](../CONTRIBUTING.md)
- [ ] I have signed the CLA (the CLA Assistant bot will prompt me if not)
