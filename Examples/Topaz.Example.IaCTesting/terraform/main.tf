terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}

  environment                     = "public"
  resource_provider_registrations = "none"
  skip_provider_registration      = true

  arm_endpoint = "https://topaz.local.dev:8899/"

  subscription_id = "00000000-0000-0000-0000-000000000001"
  tenant_id       = "50717675-3E5E-4A1E-8CB5-C62D8BE8CA48"
  client_id       = "topaz-terraform"
  client_secret   = "topaz-terraform"
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-iac-test"
  location = "West Europe"
}

resource "azurerm_storage_account" "storage" {
  name                     = "stiactest"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    environment = "test"
    owner       = "platform-team"
  }
}
