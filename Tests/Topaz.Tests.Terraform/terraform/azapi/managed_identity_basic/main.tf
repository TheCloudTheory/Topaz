data "azapi_client_config" "current" {}

resource "azapi_resource" "resource_group" {
  type      = "Microsoft.Resources/resourceGroups@2022-09-01"
  name      = "tf-api-id-rg"
  location  = "westeurope"
  parent_id = "/subscriptions/${data.azapi_client_config.current.subscription_id}"
  body      = {}
}

resource "azapi_resource" "user_assigned_identity" {
  type      = "Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31"
  name      = "tf-api-identity"
  location  = "westeurope"
  parent_id = azapi_resource.resource_group.id
  body      = {}

  response_export_values = ["name", "properties.clientId", "properties.principalId"]
}

output "identity_id" {
  value = azapi_resource.user_assigned_identity.id
}

output "client_id" {
  value = azapi_resource.user_assigned_identity.output.properties.clientId
}
