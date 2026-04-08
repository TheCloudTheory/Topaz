resource "azurerm_resource_group" "test" {
  name     = "tf-rm-id-tags-rg"
  location = "westeurope"
}

resource "azurerm_user_assigned_identity" "test" {
  name                = "tf-rm-identity-tagged"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name

  tags = {
    purpose = "testing"
  }
}

output "identity_name" {
  value = azurerm_user_assigned_identity.test.name
}
