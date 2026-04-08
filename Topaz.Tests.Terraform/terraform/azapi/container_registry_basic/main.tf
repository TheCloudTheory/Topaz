data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-acr-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "container_registry" {
  type      = "Microsoft.ContainerRegistry/registries@2023-07-01"
  name      = "tfapiacr01"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name = "Standard"
    }
    properties = {
      adminUserEnabled = false
    }
  }

  response_export_values = ["name"]
}

output "registry_id" {
  value = azapi_resource.container_registry.id
}
