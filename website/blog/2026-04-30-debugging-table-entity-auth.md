---
slug: debugging-table-entity-auth
title: "Two days chasing a SharedKey signature mismatch: how we fixed azurerm_storage_table_entity in Topaz"
authors: kamilmrzyglod
tags: [general, storage, terraform]
---

Some bugs announce themselves loudly. A null pointer in a hot path, a missing route that returns 404 to every request — the kind of thing that fails immediately and points straight at the cause. Others are quieter. They let most of the stack work correctly and only reveal themselves at the intersection of two independently correct but mutually incompatible assumptions. The `azurerm_storage_table_entity` failure was the second kind.

This post is an account of a two-day investigation into a persistent `401 Unauthorized` response on Terraform table entity operations, the four separate bugs we found along the way, and how pairing with GitHub Copilot shaped the investigation. The fix touched authentication, HTTP routing, upsert semantics, and stream lifecycle — each uncovered only after the previous one was resolved.

{/* truncate */}

## The starting point

Topaz already had table storage support: accounts, tables, entity insert and query. The `azurerm_storage_table` Terraform resource worked. What did not work was `azurerm_storage_table_entity`. Any Terraform run that tried to create a table entity failed immediately:

```
Error: creating Entity (Partition Key "pk1" / Row Key "rk1" / ...):
  executing request: unexpected status 401 (401 Unauthorized) with EOF
```

The 401 was puzzling because the same storage account, created by the same Terraform run, authenticated correctly for every ARM-level operation. Listing keys worked. Creating the table worked. Only the data-plane entity operation failed.

## The investigation begins: is the key the problem?

The first hypothesis was key mismatch. Terraform calls `listKeys` to get a storage account key and then uses it to sign data-plane requests with HMAC-SHA256. If Topaz returned a different key than it used to verify the signature, every request would fail.

To confirm or rule this out, we added diagnostic logging across the authentication path. The `TableStorageSecurityProvider` was modified to emit the full stored keys (as base64 and as raw bytes in hex), the received signature, the computed signature for both keys, the full `Authorization` header, every request header with its raw bytes, and the exact string-to-sign in hex. `ListStorageAccountKeysEndpoint` was modified to log a prefix of both keys on every call, so we could correlate what Terraform received with what Topaz verified against.

The first finding was that the keys were stable. Terraform's `listKeys` calls (there were four of them per apply — the provider is aggressive about refreshing credentials) consistently received the same key1 prefix. The key logged at verification time matched. The key mismatch hypothesis was eliminated.

## Ruling out Topaz restarts

One suspicious pattern appeared in the logs: at a certain point during a test run, ARM requests started returning `no route to host` on port 8899. This looked like the Topaz container crashing and restarting mid-test — which would regenerate the storage keys and make Terraform's cached key stale.

We checked the Docker container lifecycle carefully. The logs showed two AMQP listener errors at startup (a known benign issue with port reuse in the Docker test environment) but no crash or restart. The `no route to host` was a red herring — it appeared after Terraform had already failed and was in its cleanup phase, not before. Cross-referencing the `listKeys` timestamps with the entity request timestamps confirmed: the key prefix was identical across all four calls in the same test run. No restart, no key regeneration.

## Narrowing the scope: the isolated test

At this point we were spending time waiting for the full storage batch test to complete — roughly thirty minutes per run because Terraform retries failing requests with exponential backoff before giving up. To eliminate noise from other resources in the same test, we created a minimal isolated scenario: a single resource group, a single storage account, a single table, and a single entity. Nothing else.

```hcl
resource "azurerm_storage_table_entity" "entity" {
  storage_table_id = azurerm_storage_table.tbl.id
  partition_key    = "pk1"
  row_key          = "rk1"
  entity = {
    name = "isolated-entity"
  }
}
```

This reduced the iteration cycle and made the logs dramatically easier to read.

## The actual bug: URL encoding

With a clean log, the problem became visible. Here is what Topaz logged as the string-to-sign it computed:

```
GET\n\n\nThu, 30 Apr 2026 06:43:27 GMT\n/tfisoentityacct/isoentities(PartitionKey='pk1',%20RowKey='rk1')
```

And the TF debug log showed the URL Terraform actually sent the request to:

```
GET https://tfisoentityacct.table.storage.topaz.local.dev:8890/isoentities%28PartitionKey=%27pk1%27,%20RowKey=%27rk1%27%29
```

The entity key lookup path contains parentheses and single quotes. Terraform's go-azure-sdk encodes those: `(` → `%28`, `'` → `%27`. It signs the request using the raw encoded URL. ASP.NET Core receives the request and makes the decoded path available through `HttpRequest.Path`. When Topaz called `ToString()` on that path, it got the decoded form — `(PartitionKey='pk1',` — which is not what was signed. The HMAC computation was based on a different string than what the client used.

The fix was to read the raw request target before ASP.NET Core's decoding step using `IHttpRequestFeature.RawTarget`, which preserves the wire-format path exactly as the client sent it:

```csharp
var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget
                ?? context.Request.Path.Value
                ?? string.Empty;
var queryIndex = rawTarget.IndexOf('?');
var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;
```

This was a one-line conceptual fix that required updating the `IsRequestAuthorized` call in all fourteen table endpoint classes. The base class `TableDataPlaneEndpointBase` was refactored to accept an `HttpContext` and extract the raw path internally, so all call sites became a single-argument call.

## Bug two: the MERGE verb

With auth fixed, the next run reached Terraform's actual entity creation step — and hit a new error:

```
Request MERGE /isoentities(PartitionKey='pk1',%20RowKey='rk1') has no corresponding endpoint assigned.
```

The Azure Table Storage REST API uses HTTP `MERGE` for Insert-or-Merge operations. Topaz's `InsertOrMergeTableEntityEndpoint` only declared `POST` in its `Endpoints` array:

```csharp
public string[] Endpoints => [@"POST /^.*?\(PartitionKey='.*?',(%20|\s)?RowKey='.*?'\)$"];
```

Terraform's go-azure-sdk uses `MERGE` as the verb. The fix was straightforward — add `MERGE` to the array:

```csharp
public string[] Endpoints =>
[
    @"POST /^.*?\(PartitionKey='.*?',(%20|\s)?RowKey='.*?'\)$",
    @"MERGE /^.*?\(PartitionKey='.*?',(%20|\s)?RowKey='.*?'\)$",
];
```

## Bug three: Insert-or-Merge semantics

With routing fixed, the next run returned `404 Not Found`. The `HandleUpdateEntityRequest` path calls `DataPlane.UpdateEntity`, which throws `EntityNotFoundException` when the entity file does not exist on disk:

```csharp
if(File.Exists(entityPath) == false)
{
    throw new EntityNotFoundException();
}
```

For a `PUT` (Replace) or `PATCH` (Merge), throwing here is correct — updating a non-existent entity should fail. But `MERGE` in Table Storage semantics means *Insert-or-Merge*: create the entity if it does not exist, merge the properties if it does. The existing code had no path for that case.

We added an `UpsertEntity` method to `TableServiceDataPlane` that writes a new entity unconditionally, and a `upsert` parameter to `HandleUpdateEntityRequest` that catches `EntityNotFoundException` and falls through to the upsert path:

```csharp
catch (EntityNotFoundException) when (upsert)
{
    buffered!.Position = 0;
    DataPlane.UpsertEntity(buffered, subscriptionIdentifier, resourceGroupIdentifier,
        tableName, storageAccountName, partitionKey, rowKey);
    response.StatusCode = HttpStatusCode.NoContent;
}
```

## Bug four: the disposed stream

That `buffered!.Position = 0` line is significant. The first implementation of the upsert fallback passed the original `input` stream directly to `UpsertEntity`. The next test run returned `500 Internal Server Error` with `Cannot access a disposed object` in the Topaz logs.

`UpdateEntity` reads the body with a `StreamReader` before throwing `EntityNotFoundException`. `StreamReader` disposes its underlying stream by default when it is disposed — which happens when the `using var sr` block exits after the throw. By the time the `catch (EntityNotFoundException) when (upsert)` block ran, the stream was already closed.

The fix was to buffer the request body into a `MemoryStream` before entering the `UpdateEntity` call when `upsert` is true, and use `leaveOpen: true` in `UpdateEntity`'s `StreamReader` to avoid closing the memory stream on an unexpected throw:

```csharp
MemoryStream? buffered = null;
if (upsert)
{
    buffered = new MemoryStream();
    input.CopyTo(buffered);
    buffered.Position = 0;
    input = buffered;
}
```

## The test assertion

After all four bugs were fixed, Terraform reported `Apply complete! Resources: 4 added, 0 changed, 0 destroyed.` The test itself still failed:

```
System.InvalidOperationException : The node must be of type 'JsonValue'.
```

The `terraform output -json` response wraps each output in an envelope: `{ "partition_key": { "value": "pk1", "type": "string" } }`. The initial test assertion called `.GetValue<string>()` directly on `outputs["partition_key"]`, which is a `JsonObject`, not a `JsonValue`. The fix was a one-character change — navigate to `["value"]` first, matching every other test in the suite.

## The role of Copilot in the investigation

Copilot assisted throughout both days: adding the diagnostic logging, generating the isolated Terraform scenario and test class, and suggesting fixes as each root cause was identified. The investigation benefited from being able to express hypotheses in natural language — "could the key bytes differ even if the base64 strings match?" led immediately to logging the raw bytes in hex — and from having a second reader of the logs who could cross-reference the wire trace in the TF debug log against the canonicalization logic in the Topaz source.

The most valuable contribution was narrowing the investigation down to the URL encoding issue. The logs included a `DecodedPathSTS` variant that computed the signature using `Uri.UnescapeDataString(absolutePath)`. When that also failed to match, it confirmed the issue was not simply percent-encoded spaces — it was the full encoding of the path characters. Tracing `IHttpRequestFeature.RawTarget` was the correct escape hatch once the root cause was understood.

## What changed

| Area | Change |
|---|---|
| `TableStorageSecurityProvider` | Signs against `IHttpRequestFeature.RawTarget` (raw wire path) instead of `HttpRequest.Path` (decoded) |
| `InsertOrMergeTableEntityEndpoint` | Added `MERGE` verb alongside `POST` |
| `TableServiceDataPlane` | Added `UpsertEntity`; `UpdateEntity` uses `leaveOpen: true` |
| `TableDataPlaneEndpointBase` | `HandleUpdateEntityRequest` accepts `upsert` flag; buffers body into `MemoryStream` for fallback |
| All 14 table endpoints | Updated to pass `HttpContext` to `IsRequestAuthorized` |

The storage API coverage page now marks `azurerm_storage_table_entity` create, read, and delete as implemented. The full Terraform scenario — resource group, storage account, table, entity — runs end-to-end in roughly two minutes with no manual steps.

## Takeaway

Each of the four bugs was independently plausible and independently fixable. But they were invisible until the previous one was resolved. You cannot see that `MERGE` is unrouted if you never get past the 401. You cannot see the upsert semantics gap if `MERGE` returns 404 before reaching the data plane. You cannot see the disposed stream if the upsert path is never exercised. The debugging process was necessarily sequential — each fix revealed the next layer.

The URL encoding issue is worth calling out specifically. The Azure Table Storage SharedKey algorithm requires signing the *canonicalized resource*, which is derived from the request URL. Whether that URL is in its raw percent-encoded form or its decoded form is not a detail the specification makes obvious. ASP.NET Core's routing infrastructure silently decodes the path before most code ever sees it. `IHttpRequestFeature.RawTarget` is the correct place to read the unmodified wire path, and it is not the first thing you reach for. Getting there required enough diagnostic signal to rule out every other explanation first.
