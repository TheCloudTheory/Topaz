resource "azuread_application" "test" {
  display_name = "tf-test-sp-app"
}

resource "azuread_service_principal" "test" {
  client_id = azuread_application.test.client_id
}

output "sp_display_name" {
  value = azuread_service_principal.test.display_name
}

output "sp_object_id" {
  value = azuread_service_principal.test.object_id
}
