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

  # Force AzureRM v4 endpoint discovery against Topaz metadata.
  metadata_host = "topaz.local.dev:8899"

  # Topaz does not emulate full RP registration semantics yet.
  resource_provider_registrations = "none"
}
