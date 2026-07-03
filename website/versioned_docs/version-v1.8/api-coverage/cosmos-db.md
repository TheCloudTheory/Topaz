---
sidebar_position: 16
---

# Azure Cosmos DB

> REST API reference: [Cosmos DB Resource Provider – 2024-11-15](https://learn.microsoft.com/en-us/rest/api/cosmos-db-resource-provider/)

**Legend:** ✅ Implemented &nbsp;|&nbsp; ❌ Not implemented

## Control Plane

### Database Accounts

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}` |
| Get | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}` |
| Delete | ✅ | `DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}` |
| Update | ✅ | `PATCH /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts/{name}` |
| List By Resource Group | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.DocumentDB/databaseAccounts` |
| List | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.DocumentDB/databaseAccounts` || Check Name Availability | ✅ | `HEAD /providers/Microsoft.DocumentDB/databaseAccountNames/{name}` || List Keys | ✅ | `POST .../databaseAccounts/{name}/listKeys` |
| List Read-Only Keys | ✅ | `POST .../databaseAccounts/{name}/readonlykeys` |
| Regenerate Key | ✅ | `POST .../databaseAccounts/{name}/regenerateKey` |
| List Connection Strings | ✅ | `POST .../databaseAccounts/{name}/listConnectionStrings` |

### SQL Databases

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../databaseAccounts/{name}/sqlDatabases/{database}` |
| Get | ✅ | `GET .../databaseAccounts/{name}/sqlDatabases/{database}` |
| Delete | ✅ | `DELETE .../databaseAccounts/{name}/sqlDatabases/{database}` |
| List | ✅ | `GET .../databaseAccounts/{name}/sqlDatabases` |
| Get Throughput | ✅ | `GET .../sqlDatabases/{database}/throughputSettings/default` |
| Update Throughput | ✅ | `PUT .../sqlDatabases/{database}/throughputSettings/default` |

### SQL Containers

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../sqlDatabases/{database}/containers/{container}` |
| Get | ✅ | `GET .../sqlDatabases/{database}/containers/{container}` |
| Delete | ✅ | `DELETE .../sqlDatabases/{database}/containers/{container}` |
| List | ✅ | `GET .../sqlDatabases/{database}/containers` |
| Get Throughput | ✅ | `GET .../containers/{container}/throughputSettings/default` |
| Update Throughput | ✅ | `PUT .../containers/{container}/throughputSettings/default` |

## Data Plane

> REST API reference: [Cosmos DB REST API](https://learn.microsoft.com/en-us/rest/api/cosmos-db/)

### Databases

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Database | ✅ | `POST /{dbs}` |
| Get Database | ✅ | `GET /{dbs}/{db}` |
| Delete Database | ✅ | `DELETE /{dbs}/{db}` |
| List Databases | ✅ | `GET /{dbs}` |

### Collections

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Collection | ✅ | `POST /{dbs}/{db}/colls` |
| Get Collection | ✅ | `GET /{dbs}/{db}/colls/{coll}` |
| Replace Collection | ✅ | `PUT /{dbs}/{db}/colls/{coll}` |
| Delete Collection | ✅ | `DELETE /{dbs}/{db}/colls/{coll}` |
| List Collections | ✅ | `GET /{dbs}/{db}/colls` |

### Documents

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Document | ✅ | `POST /{dbs}/{db}/colls/{coll}/docs` |
| Get Document | ✅ | `GET /{dbs}/{db}/colls/{coll}/docs/{docId}` |
| Replace Document | ✅ | `PUT /{dbs}/{db}/colls/{coll}/docs/{docId}` |
| Patch Document | ✅ | `PATCH /{dbs}/{db}/colls/{coll}/docs/{docId}` |
| Delete Document | ✅ | `DELETE /{dbs}/{db}/colls/{coll}/docs/{docId}` |
| List Documents | ✅ | `GET /{dbs}/{db}/colls/{coll}/docs` |
| Query Documents | ✅ | `POST /{dbs}/{db}/colls/{coll}/docs` with `x-ms-documentdb-isquery: true` |
