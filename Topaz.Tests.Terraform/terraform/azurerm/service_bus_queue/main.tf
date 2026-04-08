resource "azurerm_resource_group" "test" {
  name     = "tf-rm-sbq-rg"
  location = "westeurope"
}

resource "azurerm_servicebus_namespace" "test" {
  name                = "tf-rm-sbq-ns"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "test" {
  name         = "tf-rm-queue"
  namespace_id = azurerm_servicebus_namespace.test.id
}

output "queue_name" {
  value = azurerm_servicebus_queue.test.name
}
