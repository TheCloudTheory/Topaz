data "azuread_client_config" "current" {}

resource "azuread_group" "test" {
  display_name     = "tf-test-group"
  owners           = [data.azuread_client_config.current.object_id]
  security_enabled = true
}

output "group_display_name" {
  value = azuread_group.test.display_name
}

output "group_object_id" {
  value = azuread_group.test.object_id
}
