resource "azuread_user" "test" {
  user_principal_name   = "tf-test-user@mytenant.onmicrosoft.com"
  display_name          = "Terraform Test User"
  password              = "P@ssw0rd!"
  force_password_change = false
}

output "user_upn" {
  value = azuread_user.test.user_principal_name
}

output "user_object_id" {
  value = azuread_user.test.object_id
}
