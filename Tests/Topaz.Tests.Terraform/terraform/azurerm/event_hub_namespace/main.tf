resource "azurerm_resource_group" "test" {
  name     = "tf-rm-eh-rg"
  location = "westeurope"
}

resource "azurerm_eventhub_namespace" "test" {
  name                = "tf-rm-eh-ns"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  sku                 = "Standard"
}

output "namespace_name" {
  value = azurerm_eventhub_namespace.test.name
}
