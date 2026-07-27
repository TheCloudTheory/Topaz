resource "azurerm_resource_group" "test" {
  name     = "tf-rm-acr-rg"
  location = "westeurope"
}

resource "azurerm_container_registry" "test" {
  name                = "tfrmacr01"
  resource_group_name = azurerm_resource_group.test.name
  location            = azurerm_resource_group.test.location
  sku                 = "Standard"
}

output "login_server" {
  value = azurerm_container_registry.test.login_server
}

output "admin_enabled" {
  value = azurerm_container_registry.test.admin_enabled
}
