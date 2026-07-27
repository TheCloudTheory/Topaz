resource "azurerm_resource_group" "test" {
  name     = "tf-rm-id-rg"
  location = "westeurope"
}

resource "azurerm_user_assigned_identity" "test" {
  name                = "tf-rm-identity"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
}

output "identity_name" {
  value = azurerm_user_assigned_identity.test.name
}

output "client_id" {
  value = azurerm_user_assigned_identity.test.client_id
}

output "principal_id" {
  value = azurerm_user_assigned_identity.test.principal_id
}
