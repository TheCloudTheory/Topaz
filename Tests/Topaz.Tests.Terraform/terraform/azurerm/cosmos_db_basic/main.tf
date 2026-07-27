resource "azurerm_resource_group" "cosmos_rg" {
  name     = "tf-rm-cosmos-rg"
  location = "westeurope"
}

resource "azurerm_cosmosdb_account" "cosmos" {
  name                = "tf-rm-cosmos"
  location            = azurerm_resource_group.cosmos_rg.location
  resource_group_name = azurerm_resource_group.cosmos_rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = "westeurope"
    failover_priority = 0
  }
}

output "cosmos_account_name"      { value = azurerm_cosmosdb_account.cosmos.name }
output "cosmos_account_endpoint"  { value = azurerm_cosmosdb_account.cosmos.endpoint }
