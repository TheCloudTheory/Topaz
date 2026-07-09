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

resource "azurerm_resource_group" "kv_certs_rg" {
  name     = "tf-rm-kv-certs-rg"
  location = "westeurope"
}

resource "azurerm_key_vault" "kv_certs" {
  name                      = "tfrm-kv-certs"
  location                  = azurerm_resource_group.kv_certs_rg.location
  resource_group_name       = azurerm_resource_group.kv_certs_rg.name
  tenant_id                 = data.azurerm_client_config.current.tenant_id
  sku_name                  = "standard"
  enable_rbac_authorization = true
}

resource "azurerm_key_vault_certificate" "kv_cert" {
  name         = "tfrm-kv-cert"
  key_vault_id = azurerm_key_vault.kv_certs.id

  certificate_policy {
    issuer_parameters {
      name = "Self"
    }

    key_properties {
      exportable = true
      key_size   = 2048
      key_type   = "RSA"
      reuse_key  = true
    }

    lifetime_action {
      action {
        action_type = "AutoRenew"
      }
      trigger {
        days_before_expiry = 30
      }
    }

    secret_properties {
      content_type = "application/x-pkcs12"
    }

    x509_certificate_properties {
      subject            = "CN=tfrm-kv-cert"
      validity_in_months = 12
      key_usage = [
        "cRLSign",
        "dataEncipherment",
        "digitalSignature",
        "keyAgreement",
        "keyEncipherment",
        "keyCertSign",
      ]
    }
  }
}

resource "azurerm_resource_group" "kv_keys_rg" {
  name     = "tf-rm-kv-keys-rg"
  location = "westeurope"
}

resource "azurerm_key_vault" "kv_keys" {
  name                      = "tfrm-kv-keys"
  location                  = azurerm_resource_group.kv_keys_rg.location
  resource_group_name       = azurerm_resource_group.kv_keys_rg.name
  tenant_id                 = data.azurerm_client_config.current.tenant_id
  sku_name                  = "standard"
  enable_rbac_authorization = true
}

resource "azurerm_key_vault_key" "kv_rsa_key" {
  name         = "tfrm-rsa-key"
  key_vault_id = azurerm_key_vault.kv_keys.id
  key_type     = "RSA"
  key_size     = 2048
  key_opts     = ["decrypt", "encrypt", "sign", "unwrapKey", "verify", "wrapKey"]
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

resource "azurerm_resource_group" "stor_container_rg" {
  name     = "tf-rm-stor-container-rg"
  location = "westeurope"
}

resource "azurerm_storage_account" "stor_container" {
  name                     = "tfrmstorcontainer"
  resource_group_name      = azurerm_resource_group.stor_container_rg.name
  location                 = azurerm_resource_group.stor_container_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "stor_container" {
  name               = "demo"
  storage_account_id = azurerm_storage_account.stor_container.id
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

resource "azurerm_subnet" "subnet" {
  name                 = "tf-rm-subnet"
  resource_group_name  = azurerm_resource_group.vnet_rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.1.0/24"]
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

# ── Network Security Group ────────────────────────────────────────────────────

resource "azurerm_resource_group" "nsg_rg" {
  name     = "tf-rm-nsg-rg"
  location = "westeurope"
}

resource "azurerm_network_security_group" "nsg" {
  name                = "tf-rm-nsg"
  location            = azurerm_resource_group.nsg_rg.location
  resource_group_name = azurerm_resource_group.nsg_rg.name
}

resource "azurerm_network_security_group" "nsg_tagged" {
  name                = "tf-rm-nsg-tagged"
  location            = azurerm_resource_group.nsg_rg.location
  resource_group_name = azurerm_resource_group.nsg_rg.name
  tags = {
    environment = "test"
  }
}

# ── Load Balancer ──────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "lb_rg" {
  name     = "tf-rm-lb-rg"
  location = "westeurope"
}

resource "azurerm_lb" "lb" {
  name                = "tf-rm-lb"
  location            = azurerm_resource_group.lb_rg.location
  resource_group_name = azurerm_resource_group.lb_rg.name
  sku                 = "Standard"
}

resource "azurerm_lb" "lb_basic" {
  name                = "tf-rm-lb-basic"
  location            = azurerm_resource_group.lb_rg.location
  resource_group_name = azurerm_resource_group.lb_rg.name
  sku                 = "Basic"
}

resource "azurerm_lb" "lb_tagged" {
  name                = "tf-rm-lb-tagged"
  location            = azurerm_resource_group.lb_rg.location
  resource_group_name = azurerm_resource_group.lb_rg.name
  sku                 = "Standard"
  tags = {
    environment = "test"
    team        = "platform"
  }
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
output "kv_rsa_key_name"            { value = azurerm_key_vault_key.kv_rsa_key.name }
output "kv_rsa_key_id"              { value = azurerm_key_vault_key.kv_rsa_key.id }
output "kv_cert_name"               { value = azurerm_key_vault_certificate.kv_cert.name }
output "kv_cert_secret_id"          { value = azurerm_key_vault_certificate.kv_cert.secret_id }
output "kv_cert_thumbprint"         { value = azurerm_key_vault_certificate.kv_cert.thumbprint }
output "kv_cert_version"            { value = azurerm_key_vault_certificate.kv_cert.version }

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
output "stor_container_name"            { value = azurerm_storage_container.stor_container.name }

output "id_name"                    { value = azurerm_user_assigned_identity.id.name }
output "id_client_id"               { value = azurerm_user_assigned_identity.id.client_id }
output "id_principal_id"            { value = azurerm_user_assigned_identity.id.principal_id }
output "id_tags_name"               { value = azurerm_user_assigned_identity.id_tags.name }

output "vnet_name"                  { value = azurerm_virtual_network.vnet.name }
output "vnet_address_space"         { value = azurerm_virtual_network.vnet.address_space }
output "vnet_dns_name"              { value = azurerm_virtual_network.vnet_dns.name }
output "subnet_name"                { value = azurerm_subnet.subnet.name }
output "subnet_prefix"              { value = azurerm_subnet.subnet.address_prefixes[0] }
output "nsg_name"                   { value = azurerm_network_security_group.nsg.name }
output "nsg_location"               { value = azurerm_network_security_group.nsg.location }
output "nsg_tagged_name"            { value = azurerm_network_security_group.nsg_tagged.name }

output "lb_name"                    { value = azurerm_lb.lb.name }
output "lb_sku"                     { value = azurerm_lb.lb.sku }
output "lb_basic_name"              { value = azurerm_lb.lb_basic.name }
output "lb_basic_sku"               { value = azurerm_lb.lb_basic.sku }
output "lb_tagged_name"             { value = azurerm_lb.lb_tagged.name }
output "lb_tagged_env"              { value = azurerm_lb.lb_tagged.tags["environment"] }
output "lb_tagged_team"             { value = azurerm_lb.lb_tagged.tags["team"] }

# ── Network Interface + Public IP ─────────────────────────────────────────────

resource "azurerm_resource_group" "nic_rg" {
  name     = "tf-rm-nic-rg"
  location = "westeurope"
}

resource "azurerm_public_ip" "pip" {
  name                = "tf-rm-pip"
  location            = azurerm_resource_group.nic_rg.location
  resource_group_name = azurerm_resource_group.nic_rg.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

resource "azurerm_network_interface" "nic" {
  name                = "tf-rm-nic"
  location            = azurerm_resource_group.nic_rg.location
  resource_group_name = azurerm_resource_group.nic_rg.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.subnet.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.pip.id
  }
}

output "nic_name"     { value = azurerm_network_interface.nic.name }
output "nic_location" { value = azurerm_network_interface.nic.location }
output "pip_name"     { value = azurerm_public_ip.pip.name }
output "pip_location" { value = azurerm_public_ip.pip.location }

output "acr_login_server"           { value = azurerm_container_registry.acr.login_server }
output "acr_admin_enabled"          { value = azurerm_container_registry.acr.admin_enabled }
output "acr_admin_registry_name"    { value = azurerm_container_registry.acr_admin.name }

# ── Virtual Machine ────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "vm_rg" {
  name     = "tf-rm-vm-rg"
  location = "westeurope"
}

resource "azurerm_linux_virtual_machine" "vm" {
  name                            = "tf-rm-vm"
  resource_group_name             = azurerm_resource_group.vm_rg.name
  location                        = azurerm_resource_group.vm_rg.location
  size                            = "Standard_D2s_v3"
  admin_username                  = "adminuser"
  admin_password                  = "Admin1234!@#"
  disable_password_authentication = false

  network_interface_ids = [
    azurerm_network_interface.nic.id
  ]

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Premium_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-jammy"
    sku       = "22_04-lts"
    version   = "latest"
  }
}

output "vm_name"     { value = azurerm_linux_virtual_machine.vm.name }
output "vm_location" { value = azurerm_linux_virtual_machine.vm.location }

# ── Managed Disk ───────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "disk_rg" {
  name     = "tf-rm-disk-rg"
  location = "westeurope"
}

resource "azurerm_managed_disk" "disk" {
  name                 = "tf-rm-disk"
  resource_group_name  = azurerm_resource_group.disk_rg.name
  location             = azurerm_resource_group.disk_rg.location
  storage_account_type = "Premium_LRS"
  create_option        = "Empty"
  disk_size_gb         = 32
}

output "disk_name" { value = azurerm_managed_disk.disk.name }
output "disk_sku"  { value = azurerm_managed_disk.disk.storage_account_type }

# ── App Service Plan ───────────────────────────────────────────────────────────

resource "azurerm_resource_group" "asp_rg" {
  name     = "tf-rm-asp-rg"
  location = "westeurope"
}

resource "azurerm_service_plan" "asp_basic" {
  name                = "tf-rm-asp-basic"
  resource_group_name = azurerm_resource_group.asp_rg.name
  location            = azurerm_resource_group.asp_rg.location
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_service_plan" "asp_standard" {
  name                = "tf-rm-asp-standard"
  resource_group_name = azurerm_resource_group.asp_rg.name
  location            = azurerm_resource_group.asp_rg.location
  os_type             = "Linux"
  sku_name            = "S1"
}

output "asp_basic_name"      { value = azurerm_service_plan.asp_basic.name }
output "asp_basic_sku"       { value = azurerm_service_plan.asp_basic.sku_name }
output "asp_standard_name"   { value = azurerm_service_plan.asp_standard.name }
output "asp_standard_sku"    { value = azurerm_service_plan.asp_standard.sku_name }

# ── SQL Server ────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "sql_rg" {
  name     = "tf-rm-sql-rg"
  location = "westeurope"
}

resource "azurerm_mssql_server" "sql" {
  name                         = "tf-rm-sql"
  resource_group_name          = azurerm_resource_group.sql_rg.name
  location                     = azurerm_resource_group.sql_rg.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = "SqlAdmin1234!@#"
}

output "sql_server_name"  { value = azurerm_mssql_server.sql.name }
output "sql_server_fqdn"  { value = azurerm_mssql_server.sql.fully_qualified_domain_name }

resource "azurerm_mssql_database" "sqldb" {
  name      = "tf-rm-sql-db"
  server_id = azurerm_mssql_server.sql.id
}

output "sql_db_name"      { value = azurerm_mssql_database.sqldb.name }
output "sql_db_collation" { value = azurerm_mssql_database.sqldb.collation }

# ── Cosmos DB ─────────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "cosmos_rg" {
  name     = "tf-rm-cosmos-rg"
  location = "westeurope"
}

resource "azurerm_cosmosdb_account" "cosmos" {
  name                = "tf-rm-cosmos"
  location            = azurerm_resource_group.cosmos_rg.location
  resource_group_name = azurerm_resource_group.cosmos_rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = "westeurope"
    failover_priority = 0
  }
}

resource "azurerm_cosmosdb_sql_database" "cosmos_db" {
  name                = "tf-rm-cosmos-db"
  resource_group_name = azurerm_resource_group.cosmos_rg.name
  account_name        = azurerm_cosmosdb_account.cosmos.name
}

resource "azurerm_cosmosdb_sql_container" "cosmos_container" {
  name                = "tf-rm-cosmos-container"
  resource_group_name = azurerm_resource_group.cosmos_rg.name
  account_name        = azurerm_cosmosdb_account.cosmos.name
  database_name       = azurerm_cosmosdb_sql_database.cosmos_db.name
  partition_key_paths = ["/pk"]
  throughput          = 400
}

output "cosmos_account_name"          { value = azurerm_cosmosdb_account.cosmos.name }
output "cosmos_account_endpoint"      { value = azurerm_cosmosdb_account.cosmos.endpoint }
output "cosmos_sql_db_name"           { value = azurerm_cosmosdb_sql_database.cosmos_db.name }
output "cosmos_sql_container_name"    { value = azurerm_cosmosdb_sql_container.cosmos_container.name }
output "cosmos_sql_container_pk"      { value = tostring(azurerm_cosmosdb_sql_container.cosmos_container.partition_key_paths[0]) }
output "cosmos_sql_container_throughput" { value = azurerm_cosmosdb_sql_container.cosmos_container.throughput }

# ── App Configuration ─────────────────────────────────────────────────────────

resource "azurerm_resource_group" "appconfig_rg" {
  name     = "tf-rm-appconfig-rg"
  location = "westeurope"
}

resource "azurerm_app_configuration" "appconfig" {
  name                = "tf-rm-appconfig"
  resource_group_name = azurerm_resource_group.appconfig_rg.name
  location            = azurerm_resource_group.appconfig_rg.location
  sku                 = "free"
}

output "appconfig_name"     { value = azurerm_app_configuration.appconfig.name }
output "appconfig_endpoint" { value = azurerm_app_configuration.appconfig.endpoint }
output "appconfig_sku"      { value = azurerm_app_configuration.appconfig.sku }

# ── Log Analytics ─────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "loganalytics_rg" {
  name     = "tf-rm-loganalytics-rg"
  location = "westeurope"
}

resource "azurerm_log_analytics_workspace" "loganalytics" {
  name                = "tf-rm-loganalytics"
  resource_group_name = azurerm_resource_group.loganalytics_rg.name
  location            = azurerm_resource_group.loganalytics_rg.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

output "loganalytics_name"             { value = azurerm_log_analytics_workspace.loganalytics.name }
output "loganalytics_workspace_id"     { value = azurerm_log_analytics_workspace.loganalytics.workspace_id }
output "loganalytics_sku"              { value = azurerm_log_analytics_workspace.loganalytics.sku }
output "loganalytics_retention_days"   { value = azurerm_log_analytics_workspace.loganalytics.retention_in_days }
