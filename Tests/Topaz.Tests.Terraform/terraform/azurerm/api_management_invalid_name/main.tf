resource "azurerm_resource_group" "test" {
  name     = "tf-rm-apim-invalid-rg"
  location = "westeurope"
}

# Name starts with a digit — violates Topaz's name validation regex.
# Topaz returns 400 Bad Request, causing the apply to fail.
resource "azurerm_api_management" "test" {
  name                = "1invalid-apim"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  publisher_name      = "Topaz Tests"
  publisher_email     = "admin@topaz.local.dev"
  sku_name            = "Developer_1"
}
