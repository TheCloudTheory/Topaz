resource "azurerm_resource_group" "test" {
  name     = "tf-rm-lb-rg"
  location = "westeurope"
}

resource "azurerm_lb" "test" {
  name                = "tf-rm-lb"
  location            = azurerm_resource_group.test.location
  resource_group_name = azurerm_resource_group.test.name
  sku                 = "Standard"
}

output "lb_name" {
  value = azurerm_lb.test.name
}

output "lb_id" {
  value = azurerm_lb.test.id
}

output "lb_location" {
  value = azurerm_lb.test.location
}

output "lb_sku" {
  value = azurerm_lb.test.sku
}
