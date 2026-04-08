data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-kv-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "key_vault" {
  type      = "Microsoft.KeyVault/vaults@2023-07-01"
  name      = "tf-api-kv"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id

  body = {
    properties = {
      sku = {
        family = "A"
        name   = "standard"
      }
      tenantId                = data.azapi_client_config.current.tenant_id
      enableRbacAuthorization = true
    }
  }

  response_export_values = ["name"]
}

output "vault_id" {
  value = azapi_resource.key_vault.id
}
