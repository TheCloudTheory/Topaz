terraform {
  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
  }
}

provider "azuread" {
  # Force endpoint discovery against Topaz metadata (mirrors metadata_host in azurerm.tf).
  metadata_host = "topaz.local.dev:8899"
}
