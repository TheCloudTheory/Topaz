resource "azurerm_resource_group" "test" {
  name     = "tf-rm-stor2-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "test" {
  name                     = "tfrmstoretls"
  resource_group_name      = azurerm_resource_group.test.name
  location                 = azurerm_resource_group.test.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
}

output "account_name" {
  value = azurerm_storage_account.test.name
}
