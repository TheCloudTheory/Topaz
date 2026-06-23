@description('Name of the storage account')
param storageAccountName string

@description('Azure region')
param location string = 'westeurope'

@allowed(['Standard_LRS', 'Standard_GRS', 'Premium_LRS'])
param sku string = 'Standard_LRS'

param tags object = {}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: sku
  }
  kind: 'StorageV2'
  tags: tags
}

output storageAccountId string = storage.id
output storageAccountName string = storage.name
