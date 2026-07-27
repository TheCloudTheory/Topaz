data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "test" {
  name     = "tf-rm-kv-rg"
  location = "westeurope"
}

resource "azurerm_key_vault" "test" {
  name                      = "tfrm-kv-test"
  location                  = azurerm_resource_group.test.location
  resource_group_name       = azurerm_resource_group.test.name
  tenant_id                 = data.azurerm_client_config.current.tenant_id
  sku_name                  = "standard"
  enable_rbac_authorization = true
}

output "vault_name" {
  value = azurerm_key_vault.test.name
}

output "vault_uri" {
  value = azurerm_key_vault.test.vault_uri
}
