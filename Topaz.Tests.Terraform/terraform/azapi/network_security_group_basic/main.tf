data "azapi_client_config" "current" {}

resource "azapi_resource" "nsg_resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-nsg-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "nsg" {
  type      = "Microsoft.Network/networkSecurityGroups@2023-09-01"
  name      = "tf-api-nsg"
  location  = "westeurope"
  parent_id = azapi_resource.nsg_resource_group.id
  body      = {}

  response_export_values = ["name", "type", "properties.provisioningState"]
}

output "nsg_id"                 { value = azapi_resource.nsg.id }
output "nsg_provisioning_state" { value = azapi_resource.nsg.output.properties.provisioningState }
