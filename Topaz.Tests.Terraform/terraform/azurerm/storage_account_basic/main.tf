resource "azurerm_resource_group" "test" {
  name     = "tf-rm-stor-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "test" {
  name                     = "tfrmstorageacct"
  resource_group_name      = azurerm_resource_group.test.name
  location                 = azurerm_resource_group.test.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

output "account_name" {
  value = azurerm_storage_account.test.name
}

output "primary_blob_endpoint" {
  value = azurerm_storage_account.test.primary_blob_endpoint
}
