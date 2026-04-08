data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-sb-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "servicebus_namespace" {
  type      = "Microsoft.ServiceBus/namespaces@2022-10-01-preview"
  name      = "tf-api-sb-ns"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name = "Standard"
      tier = "Standard"
    }
  }

  response_export_values = ["name"]
}

output "namespace_id" {
  value = azapi_resource.servicebus_namespace.id
}
