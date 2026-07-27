data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-vnet-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "virtual_network" {
  type      = "Microsoft.Network/virtualNetworks@2023-09-01"
  name      = "tf-api-vnet"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    properties = {
      addressSpace = {
        addressPrefixes = ["10.0.0.0/16"]
      }
    }
  }

  response_export_values = ["name", "properties.addressSpace"]
}

output "vnet_id" {
  value = azapi_resource.virtual_network.id
}
