resource "azurerm_resource_group" "rg" {
  name     = "tf-iso-stor-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "sa" {
  name                     = "tfisoentityacct"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_table" "tbl" {
  name                 = "isoentities"
  storage_account_name = azurerm_storage_account.sa.name
}

resource "azurerm_storage_table_entity" "entity" {
  storage_table_id = azurerm_storage_table.tbl.id
  partition_key    = "pk1"
  row_key          = "rk1"
  entity = {
    name = "isolated-entity"
  }
}

output "partition_key" {
  value = azurerm_storage_table_entity.entity.partition_key
}

output "row_key" {
  value = azurerm_storage_table_entity.entity.row_key
}
