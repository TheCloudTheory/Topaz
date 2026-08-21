# Release Notes - v1.11

## New Features

### Azure Resource Seeding (`seed` command)
Import resources from a real Azure subscription into Topaz using the new `seed` CLI command. The `Topaz.Importer` module handles seeding across all major resource types: App Service Plans & Sites, API Management, App Configuration, Container Registry, Cosmos DB, Disks, Event Hubs, Key Vault, Load Balancers, Log Analytics, Managed Identity, Redis Cache, Resource Groups, Service Bus, SQL Server & Databases, Storage Accounts, Virtual Machines, Availability Sets, and Virtual Networks (NICs, NSGs, Private Endpoints, Public IPs).

### Redis Cache Management (Portal)
The web portal now includes full Redis Cache management: list caches, view overview details, manage IAM, and update tags.

### Context Switching (`context` command)
New CLI command to switch Azure CLI contexts, enabling multi-tenant and multi-subscription workflows without leaving Topaz.

### App Configuration Soft-Delete Purge Scheduler
Topaz now runs a background scheduler (`AppConfigurationSoftDeletePurgeScheduler`) that automatically purges soft-deleted App Configuration stores past their scheduled purge date, matching Azure's behaviour.

### App Configuration Purge Protection
Topaz now enforces purge protection for App Configuration stores. Stores with purge protection enabled cannot be purged even after soft-deletion, matching Azure's behaviour. The `PurgeConfigurationStoreEndpoint` and `AppConfigurationServiceControlPlane` were updated to validate and reject purge requests accordingly.

### App Configuration data plane RBAC enforcement
Topaz now correctly enforces RBAC authorization for all implemented data-plane operations in App Configuration.

## Bug Fixes

- Fixed nullable `Content` handling and added async overload in the internal Router, preventing potential null reference errors on certain request paths.
- Improved App Configuration store update logic: update and validation are now handled by `ConfigurationStoreFullResource.UpdateFromRequest()` and `Validate()`, ensuring invalid SKU downgrades and other bad requests are rejected with a proper `BadRequest` response.
