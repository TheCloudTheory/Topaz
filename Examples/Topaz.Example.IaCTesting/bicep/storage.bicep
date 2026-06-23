@description('Name of the storage account')
param storageAccountName string = 'stbiceptest'

@description('Azure region')
param location string = 'westeurope'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: {
    environment: 'test'
    owner: 'platform-team'
  }
}

output storageAccountId string = storage.id
