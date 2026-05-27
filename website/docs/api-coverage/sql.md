---
sidebar_position: 15
---

# Azure SQL

> REST API reference: [SQL – 2023-08-01](https://learn.microsoft.com/en-us/rest/api/sql/?view=rest-sql-2023-08-01)

**Legend:** ✅ Implemented &nbsp;|&nbsp; ❌ Not implemented

## Control Plane

### Servers

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}` |
| Get | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}` |
| Delete | ✅ | `DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}` |
| Update | ✅ | `PATCH /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers/{name}` |
| List By Resource Group | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Sql/servers` |
| List | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.Sql/servers` |
| Check Name Availability | ❌ | |
| Import Database | ❌ | |

### Databases

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../servers/{server}/databases/{database}` |
| Get | ✅ | `GET .../servers/{server}/databases/{database}` |
| Delete | ✅ | `DELETE .../servers/{server}/databases/{database}` |
| Update | ✅ | `PATCH .../servers/{server}/databases/{database}` |
| List By Server | ✅ | `GET .../servers/{server}/databases` |
| Export | ❌ | |
| Import | ❌ | |
| Rename | ❌ | |
| Failover | ❌ | |

### Connection Policies

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../servers/{server}/connectionPolicies/{policyName}` |
| Get | ✅ | `GET .../servers/{server}/connectionPolicies/{policyName}` |

### SQL Vulnerability Assessments

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../servers/{server}/sqlVulnerabilityAssessments/{assessmentName}` |
| Get | ✅ | `GET .../servers/{server}/sqlVulnerabilityAssessments/{assessmentName}` |

### Restorable Dropped Databases

| Operation | Status | Notes |
|-----------|--------|-------|
| List By Server | ✅ | `GET .../servers/{server}/restorableDroppedDatabases` — always returns empty list |

### Transparent Data Encryption

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | `GET .../databases/{db}/transparentDataEncryption/current` — always returns Enabled |
| Create Or Update | ✅ | `PUT .../databases/{db}/transparentDataEncryption/current` |

### Database Security Alert Policies

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | `GET .../databases/{db}/securityAlertPolicies/{policyName}` — returns Disabled state |
| Create Or Update | ✅ | `PUT .../databases/{db}/securityAlertPolicies/{policyName}` |

### Backup Long Term Retention Policies

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | `GET .../databases/{db}/backupLongTermRetentionPolicies/{policyName}` |
| Create Or Update | ✅ | `PUT .../databases/{db}/backupLongTermRetentionPolicies/{policyName}` |

### Backup Short Term Retention Policies

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | `GET .../databases/{db}/backupShortTermRetentionPolicies/{policyName}` |
| Create Or Update | ✅ | `PUT .../databases/{db}/backupShortTermRetentionPolicies/{policyName}` |

### Firewall Rules

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ❌ | |
| Get | ❌ | |
| Delete | ❌ | |
| List By Server | ❌ | |

### Elastic Pools

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ❌ | |
| Get | ❌ | |
| Delete | ❌ | |
| Update | ❌ | |
| List By Server | ❌ | |
