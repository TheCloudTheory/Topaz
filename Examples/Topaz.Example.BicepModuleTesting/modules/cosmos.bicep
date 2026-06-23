@description('Name of the Cosmos DB account')
param accountName string

@description('Primary Azure region')
param location string = 'westeurope'

@description('Optional secondary region for geo-redundancy testing')
param secondaryLocation string = ''

param tags object = {}

var locations = empty(secondaryLocation)
  ? [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  : [
      {
        locationName: location
        failoverPriority: 0
      }
      {
        locationName: secondaryLocation
        failoverPriority: 1
      }
    ]

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: locations
  }
  tags: tags
}

output accountEndpoint string = cosmosAccount.properties.documentEndpoint
output accountId string = cosmosAccount.id
