resource "azurerm_resource_group" "test" {
  name     = "tf-rm-acr-admin-rg"
  location = "westeurope"
}

resource "azurerm_container_registry" "test" {
  name                = "tfrmacradmin"
  resource_group_name = azurerm_resource_group.test.name
  location            = azurerm_resource_group.test.location
  sku                 = "Standard"
  admin_enabled       = true
}

output "registry_name" {
  value = azurerm_container_registry.test.name
}
