resource "azurerm_resource_group" "test" {
  name     = "tf-rm-rg"
  location = "westeurope"
}

output "name" {
  value = azurerm_resource_group.test.name
}

output "location" {
  value = azurerm_resource_group.test.location
}
