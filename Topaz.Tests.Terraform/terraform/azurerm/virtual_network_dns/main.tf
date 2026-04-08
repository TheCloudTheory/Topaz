resource "azurerm_resource_group" "test" {
  name     = "tf-rm-vnet-dns-rg"
  location = "westeurope"
}

resource "azurerm_virtual_network" "test" {
  name                = "tf-rm-vnet-dns"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  address_space       = ["10.1.0.0/16"]
  dns_servers         = ["8.8.8.8", "8.8.4.4"]
}

output "vnet_name" {
  value = azurerm_virtual_network.test.name
}
