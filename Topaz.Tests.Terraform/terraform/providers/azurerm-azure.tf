terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 4.67.0"
    }
  }
}

# Real Azure provider — no Topaz overrides.
# Credentials are read from the standard ARM_* environment variables:
#   ARM_SUBSCRIPTION_ID, ARM_TENANT_ID, ARM_CLIENT_ID, ARM_CLIENT_SECRET
#
# The service principal must have at minimum:
#   - Contributor on the target subscription
#   - Key Vault Crypto Officer + Key Vault Certificates Officer on any Key Vault
#     created during the run (or Owner/User Access Administrator to self-assign them).
provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
}
