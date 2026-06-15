data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-lb-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "load_balancer" {
  type      = "Microsoft.Network/loadBalancers@2023-09-01"
  name      = "tf-rm-lb"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name = "Standard"
    }
    properties = {}
  }

  response_export_values = ["name", "sku"]
}

output "lb_id" {
  value = azapi_resource.load_balancer.id
}
