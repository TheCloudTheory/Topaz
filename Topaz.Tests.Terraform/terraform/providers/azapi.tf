terraform {
  required_providers {
    azapi = {
      source  = "azure/azapi"
      version = "~> 2.0"
    }
  }
}

provider "azapi" {
  use_msi  = false
  use_oidc = false
  use_cli  = true
}
