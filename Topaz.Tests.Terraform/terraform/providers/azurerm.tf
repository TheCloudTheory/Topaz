terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.0.0"
    }
  }
}

provider "azurerm" {
  features {}
  skip_provider_registration = true
  use_msi  = false
  use_oidc = false
  use_cli  = true
}
