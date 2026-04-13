---
sidebar_position: 4
---

# storage account show-connection-string

Shows a connection string for the given storage account.

## Options
* `-n|--name` - (Required) storage account name

## Examples

### List access keys
```bash
$ topaz storage account show-connection-string --name "salocal"

{
    "connectionString": "DefaultEndpointsProtocol=http;AccountName=salocal;AccountKey=6B2E10C92A6E17F0D516C0E015AFD0C6B6E26B6A;BlobEndpoint=http://127.0.0.1:8891/salocal;QueueEndpoint=http: //localhost:8899;TableEndpoint=http://localhost:8890/storage/salocal;"
}
```

## Remarks
The command displays a connection string for the first access key available for the storage account.
