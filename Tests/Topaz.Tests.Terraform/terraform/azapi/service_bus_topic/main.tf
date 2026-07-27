data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-sbt-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "servicebus_namespace" {
  type      = "Microsoft.ServiceBus/namespaces@2022-10-01-preview"
  name      = "tf-api-sbt-ns"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name = "Standard"
      tier = "Standard"
    }
  }
}

resource "azapi_resource" "topic" {
  type      = "Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview"
  name      = "tf-api-topic"
  parent_id = azapi_resource.servicebus_namespace.id
  body      = {}

  response_export_values = ["name"]
}

output "topic_id" {
  value = azapi_resource.topic.id
}
