terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
  }
}

provider "azurerm" {
  features {}
  environment = "Topaz"
  metadata_host = "topaz.local.dev:8899"
  resource_provider_registrations = "none"
  use_msi  = false
  use_oidc = false
  use_cli  = true
}
