resource "azurerm_resource_group" "test" {
  name     = "tf-rm-vnet-rg"
  location = "westeurope"
}

resource "azurerm_virtual_network" "test" {
  name                = "tf-rm-vnet"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  address_space       = ["10.0.0.0/16"]
}

output "vnet_name" {
  value = azurerm_virtual_network.test.name
}

output "address_space" {
  value = azurerm_virtual_network.test.address_space
}
