data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-tagged-rg"
  location  = "northeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}

  tags = {
    environment = "test"
  }

  response_export_values = ["name"]
}

output "resource_group_id" {
  value = azapi_resource.resource_group.id
}
