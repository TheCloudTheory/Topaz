resource "azurerm_resource_group" "test" {
  name     = "tf-rm-tagged-rg"
  location = "northeurope"

  tags = {
    environment = "test"
    team        = "platform"
  }
}

output "tag_environment" {
  value = azurerm_resource_group.test.tags["environment"]
}
