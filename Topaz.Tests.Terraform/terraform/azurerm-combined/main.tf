# Combined AzureRM scenario — all 17 individual scenarios merged into one workspace.
# Running a single apply+destroy instead of 17 saves ~32 azurerm v4 provider startups.

data "azurerm_client_config" "current" {}

# ── Resource Groups ────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "rg_basic" {
  name     = "tf-rm-rg"
  location = "westeurope"
}

resource "azurerm_resource_group" "rg_tags" {
  name     = "tf-rm-tagged-rg"
  location = "northeurope"
  tags = {
    environment = "test"
    team        = "platform"
  }
}

# ── Key Vault ──────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "kv_basic_rg" {
  name     = "tf-rm-kv-rg"
  location = "westeurope"
}

resource "azurerm_key_vault" "kv_basic" {
  name                      = "tfrm-kv-test"
  location                  = azurerm_resource_group.kv_basic_rg.location
  resource_group_name       = azurerm_resource_group.kv_basic_rg.name
  tenant_id                 = data.azurerm_client_config.current.tenant_id
  sku_name                  = "standard"
  enable_rbac_authorization = true
}

resource "azurerm_resource_group" "kv_sd_rg" {
  name     = "tf-rm-kv-sd-rg"
  location = "westeurope"
}

resource "azurerm_key_vault" "kv_sd" {
  name                       = "tfrm-kv-sd"
  location                   = azurerm_resource_group.kv_sd_rg.location
  resource_group_name        = azurerm_resource_group.kv_sd_rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  enable_rbac_authorization  = true
}

# ── Event Hub ──────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "eh_ns_rg" {
  name     = "tf-rm-eh-rg"
  location = "westeurope"
}

resource "azurerm_eventhub_namespace" "eh_ns" {
  name                = "tf-rm-eh-ns"
  location            = azurerm_resource_group.eh_ns_rg.location
  resource_group_name = azurerm_resource_group.eh_ns_rg.name
  sku                 = "Standard"
}

resource "azurerm_resource_group" "ehub_rg" {
  name     = "tf-rm-ehub-rg"
  location = "westeurope"
}

resource "azurerm_eventhub_namespace" "ehub_ns" {
  name                = "tf-rm-ehub-ns"
  location            = azurerm_resource_group.ehub_rg.location
  resource_group_name = azurerm_resource_group.ehub_rg.name
  sku                 = "Standard"
}

resource "azurerm_eventhub" "ehub" {
  name                = "tf-rm-eventhub"
  namespace_name      = azurerm_eventhub_namespace.ehub_ns.name
  resource_group_name = azurerm_resource_group.ehub_rg.name
  partition_count     = 2
  message_retention   = 1
}

# ── Service Bus ────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "sb_ns_rg" {
  name     = "tf-rm-sb-rg"
  location = "westeurope"
}

resource "azurerm_servicebus_namespace" "sb_ns" {
  name                = "tf-rm-sb-ns"
  location            = azurerm_resource_group.sb_ns_rg.location
  resource_group_name = azurerm_resource_group.sb_ns_rg.name
  sku                 = "Standard"
}

resource "azurerm_resource_group" "sb_q_rg" {
  name     = "tf-rm-sbq-rg"
  location = "westeurope"
}

resource "azurerm_servicebus_namespace" "sb_q_ns" {
  name                = "tf-rm-sbq-ns"
  location            = azurerm_resource_group.sb_q_rg.location
  resource_group_name = azurerm_resource_group.sb_q_rg.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "sb_q" {
  name         = "tf-rm-queue"
  namespace_id = azurerm_servicebus_namespace.sb_q_ns.id
}

resource "azurerm_resource_group" "sb_t_rg" {
  name     = "tf-rm-sbt-rg"
  location = "westeurope"
}

resource "azurerm_servicebus_namespace" "sb_t_ns" {
  name                = "tf-rm-sbt-ns"
  location            = azurerm_resource_group.sb_t_rg.location
  resource_group_name = azurerm_resource_group.sb_t_rg.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_topic" "sb_t" {
  name         = "tf-rm-topic"
  namespace_id = azurerm_servicebus_namespace.sb_t_ns.id
}

# ── Storage ────────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "stor_rg" {
  name     = "tf-rm-stor-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "stor" {
  name                     = "tfrmstorageacct"
  resource_group_name      = azurerm_resource_group.stor_rg.name
  location                 = azurerm_resource_group.stor_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_resource_group" "stor_tls_rg" {
  name     = "tf-rm-stor2-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "stor_tls" {
  name                     = "tfrmstoretls"
  resource_group_name      = azurerm_resource_group.stor_tls_rg.name
  location                 = azurerm_resource_group.stor_tls_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
}

resource "azurerm_resource_group" "stor_table_rg" {
  name     = "tf-rm-stor-table-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "stor_table" {
  name                     = "tfrmstortableacct"
  resource_group_name      = azurerm_resource_group.stor_table_rg.name
  location                 = azurerm_resource_group.stor_table_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_table" "stor_table_entities" {
  name                 = "tfrmentities"
  storage_account_name = azurerm_storage_account.stor_table.name
}

resource "azurerm_storage_table_entity" "stor_table_entity" {
  storage_table_id = azurerm_storage_table.stor_table_entities.id
  partition_key    = "pk1"
  row_key          = "rk1"
  entity = {
    name = "terraform-entity"
  }
}

# ── Managed Identity ───────────────────────────────────────────────────────────

resource "azurerm_resource_group" "id_rg" {
  name     = "tf-rm-id-rg"
  location = "westeurope"
}

resource "azurerm_user_assigned_identity" "id" {
  name                = "tf-rm-identity"
  location            = azurerm_resource_group.id_rg.location
  resource_group_name = azurerm_resource_group.id_rg.name
}

resource "azurerm_resource_group" "id_tags_rg" {
  name     = "tf-rm-id-tags-rg"
  location = "westeurope"
}

resource "azurerm_user_assigned_identity" "id_tags" {
  name                = "tf-rm-identity-tagged"
  location            = azurerm_resource_group.id_tags_rg.location
  resource_group_name = azurerm_resource_group.id_tags_rg.name
  tags = {
    purpose = "testing"
  }
}

# ── Virtual Network ────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "vnet_rg" {
  name     = "tf-rm-vnet-rg"
  location = "westeurope"
}

resource "azurerm_virtual_network" "vnet" {
  name                = "tf-rm-vnet"
  location            = azurerm_resource_group.vnet_rg.location
  resource_group_name = azurerm_resource_group.vnet_rg.name
  address_space       = ["10.0.0.0/16"]
}

resource "azurerm_resource_group" "vnet_dns_rg" {
  name     = "tf-rm-vnet-dns-rg"
  location = "westeurope"
}

resource "azurerm_virtual_network" "vnet_dns" {
  name                = "tf-rm-vnet-dns"
  location            = azurerm_resource_group.vnet_dns_rg.location
  resource_group_name = azurerm_resource_group.vnet_dns_rg.name
  address_space       = ["10.1.0.0/16"]
  dns_servers         = ["8.8.8.8", "8.8.4.4"]
}

# ── Container Registry ─────────────────────────────────────────────────────────

resource "azurerm_resource_group" "acr_rg" {
  name     = "tf-rm-acr-rg"
  location = "westeurope"
}

resource "azurerm_container_registry" "acr" {
  name                = "tfrmacr01"
  resource_group_name = azurerm_resource_group.acr_rg.name
  location            = azurerm_resource_group.acr_rg.location
  sku                 = "Standard"
}

resource "azurerm_resource_group" "acr_admin_rg" {
  name     = "tf-rm-acr-admin-rg"
  location = "westeurope"
}

resource "azurerm_container_registry" "acr_admin" {
  name                = "tfrmacradmin"
  resource_group_name = azurerm_resource_group.acr_admin_rg.name
  location            = azurerm_resource_group.acr_admin_rg.location
  sku                 = "Standard"
  admin_enabled       = true
}

# ── Outputs ────────────────────────────────────────────────────────────────────

output "rg_basic_name"              { value = azurerm_resource_group.rg_basic.name }
output "rg_basic_location"          { value = azurerm_resource_group.rg_basic.location }
output "rg_tags_environment"        { value = azurerm_resource_group.rg_tags.tags["environment"] }

output "kv_basic_vault_name"        { value = azurerm_key_vault.kv_basic.name }
output "kv_basic_vault_uri"         { value = azurerm_key_vault.kv_basic.vault_uri }
output "kv_sd_vault_name"           { value = azurerm_key_vault.kv_sd.name }

output "eh_ns_namespace_name"       { value = azurerm_eventhub_namespace.eh_ns.name }
output "ehub_name"                  { value = azurerm_eventhub.ehub.name }
output "ehub_partition_count"       { value = azurerm_eventhub.ehub.partition_count }

output "sb_ns_namespace_name"       { value = azurerm_servicebus_namespace.sb_ns.name }
output "sb_q_queue_name"            { value = azurerm_servicebus_queue.sb_q.name }
output "sb_t_topic_name"            { value = azurerm_servicebus_topic.sb_t.name }

output "stor_account_name"          { value = azurerm_storage_account.stor.name }
output "stor_primary_blob_endpoint" { value = azurerm_storage_account.stor.primary_blob_endpoint }
output "stor_tls_account_name"      { value = azurerm_storage_account.stor_tls.name }
output "stor_table_entity_partition_key" { value = azurerm_storage_table_entity.stor_table_entity.partition_key }
output "stor_table_entity_row_key"       { value = azurerm_storage_table_entity.stor_table_entity.row_key }

output "id_name"                    { value = azurerm_user_assigned_identity.id.name }
output "id_client_id"               { value = azurerm_user_assigned_identity.id.client_id }
output "id_principal_id"            { value = azurerm_user_assigned_identity.id.principal_id }
output "id_tags_name"               { value = azurerm_user_assigned_identity.id_tags.name }

output "vnet_name"                  { value = azurerm_virtual_network.vnet.name }
output "vnet_address_space"         { value = azurerm_virtual_network.vnet.address_space }
output "vnet_dns_name"              { value = azurerm_virtual_network.vnet_dns.name }

output "acr_login_server"           { value = azurerm_container_registry.acr.login_server }
output "acr_admin_enabled"          { value = azurerm_container_registry.acr.admin_enabled }
output "acr_admin_registry_name"    { value = azurerm_container_registry.acr_admin.name }
