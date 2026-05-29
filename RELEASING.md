# Release Checklist

This document describes every step required to publish a new Topaz release. Follow the steps in order.
The checklist uses **vX.Y** to refer to the version being released and **vX.Z** for the next development version (e.g. releasing v1.5, next is v1.6).

---

## 1. Pre-release verification

- [ ] All tests pass: `dotnet test Topaz.sln`
- [ ] Docker image builds successfully: `./scripts/build-docker.sh arm64` (and `amd64`)
- [ ] E2E, AzureCLI, Terraform, and PowerShell tests pass against the Docker image
- [ ] The blog post for this release (weekly dev update) has been merged

---

## 2. Version metadata

### 2a. Advance the development version

In [`version.json`](version.json), bump `"version"` to the **next** development cycle:

```json
{ "version": "vX.Z-beta" }
```

### 2b. Docusaurus versioned docs snapshot

From the `website/` directory, run:

```bash
npm run docusaurus -- docs:version vX.Y
```

This copies the current `docs/` into `versioned_docs/version-vX.Y/` and adds `vX.Y` to `website/versions.json`.

> **Important:** run this command **before** editing `versions.json` or `docusaurus.config.ts`, otherwise Docusaurus will fail to start because it cannot find the versioned folder.

### 2c. Update `website/versions.json`

After the snapshot command has run, remove the **oldest** supported version (one version is dropped per release). Supported window is the three most recent releases:

```json
["vX.Y", "vX.Y-1", "vX.Y-2"]
```

### 2d. Update `website/docusaurus.config.ts`

- Add an entry for `vX.Y` in the `versions` object: `'vX.Y': { label: 'vX.Y (stable)', badge: true }`
- Remove the entry for the dropped version
- Change `lastVersion` to `'vX.Y'`

### 2e. Delete dropped versioned docs

```bash
rm -rf website/versioned_docs/version-vOLD
rm -f  website/versioned_sidebars/version-vOLD-sidebars.json
```

---

## 3. Roadmap (`website/src/pages/_roadmap-content.mdx`)

- Move the top `## vX.Y-beta` section into `## ✅ Completed` as a new `### vX.Y` subsection (above the previous release entry)
  - Add `_Released on DD Month YYYY._` beneath the `### vX.Y` heading
  - Downgrade all `###` subsection headings inside the block to `####`
- Promote `## vX.Z-beta` to the first section (it is already the next block in the file after removing `## vX.Y-beta`)
- Flip any roadmap badge from Preview → Stable for features that shipped

---

## 4. Supported services

### 4a. `README.md` — supported services table

Add any new services that shipped in this release. Use `✅` for the control plane column, `✅` for data plane if applicable, or `—` if there is no data plane:

```
| Azure New Service | ✅ | — | Preview |
```

### 4b. `website/docs/supported-services.md` — service status table

- Add new services: `ServiceName|🚧|N/A` (or `🚧|🚧` if data plane also shipped)
- Upgrade existing services from `🔜` to `🚧` when initial support ships, or from `🚧` to `✅` when fully stable

---

## 5. Known limitations (`website/docs/known-limitations.md`)

- **Remove** any limitation entry whose "Planned fix" milestone is the version being released and whose fix is confirmed shipped (verify in the control plane / endpoint code)
- **Add** new entries for any newly discovered limitations or deliberate design trade-offs introduced in this release
- Update "Planned fix" version labels: if a fix slips to a later milestone, update the heading accordingly

---

## 6. API coverage docs (`website/docs/api-coverage/`)

- For each new endpoint added in this release, flip the corresponding ❌ to ✅ in the relevant `<service>.md` file
- If a new service was added, create a new coverage page following the [Container Registry coverage](website/docs/api-coverage/container-registry.md) format

---

## 7. Compatibility page (`website/docs/compatibility.md`)

- Update any Azure SDK package versions that changed in `Topaz.Tests/E2E/`
- Update the Terraform provider pin if it changed in `Topaz.Tests.Terraform/`
- Add rows for any new SDK packages introduced (e.g. `Azure.ResourceManager.Sql`)
- If a new language SDK was published (e.g. Python `topaz-sdk`), add a new section

---

## 8. Backlog (`BACKLOG.md`)

- Remove the `<!-- TODO: ... -->` blocks that correspond to completed work so they are not re-opened as GitHub Issues

---

## 9. GitHub release

- Create a new GitHub release tagged `vX.Y.0` (or the full NerdBank-generated tag)
- Attach the published binaries (arm64, x64) for host and CLI
- Copy the blog post summary as the release description

---

## 10. Homebrew formula (`homebrew-topaz/Formula/topaz.rb`)

After the GitHub release artifacts are published and their SHA256 hashes are available:

- Bump `version "X.Y.BUILD-beta"` to match the published tag
- Update all five `url` lines (arm64 host, x64 host, arm64 CLI, x64 CLI, certificate)
- Update all five `sha256` lines

Compute hashes with:

```bash
curl -L <url> | shasum -a 256
```

---

## 11. Website build verification

```bash
cd website && npm run build
```

Confirm the build completes with `[SUCCESS] Generated static files in "build".` and no broken-link errors.

---

## 12. Post-release

- Announce on Discord (`#announcements`)
- Post the blog update link to social channels (LinkedIn, Dev.to, HN — see `topaz-strategy/socials/`)
- Open a new milestone section in `BACKLOG.md` for `vX.Z-beta` planned work
- Add the `vX.Z-beta` section to `website/src/pages/_roadmap-content.mdx` with planned features
