terraform {
  required_providers {
    azapi = {
      source  = "azure/azapi"
      version = "~> 2.0"
    }
  }
}

variable "subscription_id" {
  type = string
}

provider "azapi" {
  subscription_id = var.subscription_id
  use_msi  = false
  use_oidc = false
  use_cli  = true
}
