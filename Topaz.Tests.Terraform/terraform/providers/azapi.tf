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
  subscription_id             = var.subscription_id
  use_msi                     = false
  use_oidc                    = false
  use_cli                     = true
  disable_instance_discovery  = true

  endpoint = [{
    resource_manager_endpoint       = "https://topaz.local.dev:8899/"
    active_directory_authority_host = "https://topaz.local.dev:8899/"
    resource_manager_audience       = "https://topaz.local.dev:8899/"
  }]
}
