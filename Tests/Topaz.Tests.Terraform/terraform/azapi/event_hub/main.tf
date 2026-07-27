resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-ehub-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${var.subscription_id}"
  body      = {}
}

resource "azapi_resource" "eventhub_namespace" {
  type      = "Microsoft.EventHub/namespaces@2022-10-01-preview"
  name      = "tf-api-ehub-ns"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name     = "Standard"
      tier     = "Standard"
      capacity = 1
    }
  }
}

resource "azapi_resource" "eventhub" {
  type      = "Microsoft.EventHub/namespaces/eventhubs@2022-10-01-preview"
  name      = "tf-api-eventhub"
  parent_id = azapi_resource.eventhub_namespace.id

  body = {
    properties = {
      partitionCount         = 2
      messageRetentionInDays = 1
    }
  }

  response_export_values = ["name"]
}

output "eventhub_id" {
  value = azapi_resource.eventhub.id
}
