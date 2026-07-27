resource "azurerm_resource_group" "test" {
  name     = "tf-rm-sbt-rg"
  location = "westeurope"
}

resource "azurerm_servicebus_namespace" "test" {
  name                = "tf-rm-sbt-ns"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_topic" "test" {
  name         = "tf-rm-topic"
  namespace_id = azurerm_servicebus_namespace.test.id
}

output "topic_name" {
  value = azurerm_servicebus_topic.test.name
}
