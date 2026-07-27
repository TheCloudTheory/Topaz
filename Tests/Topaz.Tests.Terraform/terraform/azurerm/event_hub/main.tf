resource "azurerm_resource_group" "test" {
  name     = "tf-rm-ehub-rg"
  location = "westeurope"
}

resource "azurerm_eventhub_namespace" "test" {
  name                = "tf-rm-ehub-ns"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  sku                 = "Standard"
}

resource "azurerm_eventhub" "test" {
  name                = "tf-rm-eventhub"
  namespace_name      = azurerm_eventhub_namespace.test.name
  resource_group_name = azurerm_resource_group.test.name
  partition_count     = 2
  message_retention   = 1
}

output "eventhub_name" {
  value = azurerm_eventhub.test.name
}

output "partition_count" {
  value = azurerm_eventhub.test.partition_count
}
