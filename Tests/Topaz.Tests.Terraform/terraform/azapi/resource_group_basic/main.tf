data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}

  response_export_values = ["name", "location"]
}

output "resource_group_id" {
  value = azapi_resource.resource_group.id
}

output "location" {
  value = azapi_resource.resource_group.output.location
}
