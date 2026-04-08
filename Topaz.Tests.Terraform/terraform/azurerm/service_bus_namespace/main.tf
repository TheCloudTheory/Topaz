resource "azurerm_resource_group" "test" {
  name     = "tf-rm-sb-rg"
  location = "westeurope"
}

resource "azurerm_servicebus_namespace" "test" {
  name                = "tf-rm-sb-ns"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  sku                 = "Standard"
}

output "namespace_name" {
  value = azurerm_servicebus_namespace.test.name
}
