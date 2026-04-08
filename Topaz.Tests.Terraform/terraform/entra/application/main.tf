resource "azuread_application" "test" {
  display_name = "tf-test-app"
}

output "app_display_name" {
  value = azuread_application.test.display_name
}

output "app_client_id" {
  value = azuread_application.test.client_id
}
