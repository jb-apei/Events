terraform {
  required_version = ">= 1.6"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
  subscription_id = var.subscription_id
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = var.location
  tags     = var.tags
}

# Azure Verified Module: Service Bus Namespace
module "service_bus" {
  source  = "Azure/avm-res-servicebus-namespace/azurerm"
  version = "~> 0.3"

  name                = "sb-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = var.service_bus_sku

  queues = {
    identity-commands = {
      name                                    = "identity-commands"
      dead_lettering_on_message_expiration    = true
      max_delivery_count                      = 10
      default_message_ttl                     = "P14D" # 14 days
      lock_duration                           = "PT1M"  # 1 minute
      requires_duplicate_detection            = true
      duplicate_detection_history_time_window = "PT10M" # 10 minutes
    }
  }

  topics = {
    prospect-dlq = {
      name                         = "prospect-dlq"
      default_message_ttl          = "P14D"
      requires_duplicate_detection = true
    }
  }

  tags = var.tags
}

# Azure Verified Module: Event Grid Domain
module "event_grid" {
  source  = "Azure/avm-res-eventgrid-domain/azurerm"
  version = "~> 0.1"

  name                = "evgd-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  input_schema         = "CloudEventSchemaV1_0"
  public_network_access_enabled = var.environment == "dev" ? true : false

  tags = var.tags
}

# Event Grid Topics (using custom module as AVM doesn't have dedicated topic module)
module "event_grid_topics" {
  source = "./modules/event-grid-topics"

  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  project_name        = var.project_name
  environment         = var.environment

  topics = [
    "prospect-events",
    "student-events",
    "instructor-events"
  ]

  tags = var.tags
}

# Azure Verified Module: SQL Server
module "sql_server" {
  source  = "Azure/avm-res-sql-server/azurerm"
  version = "~> 0.8"

  name                = "sql-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  version                      = "12.0"

  public_network_access_enabled = var.environment == "dev" ? true : false
  minimum_tls_version           = "1.2"

  # Allow Azure services to access
  firewall_rules = var.environment == "dev" ? {
    AllowAzureServices = {
      start_ip_address = "0.0.0.0"
      end_ip_address   = "0.0.0.0"
    }
  } : {}

  tags = var.tags
}

# Transactional Database
resource "azurerm_mssql_database" "transactional" {
  name           = "db-${var.project_name}-transactional-${var.environment}"
  server_id      = module.sql_server.resource_id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  sku_name       = "Basic" # Free tier equivalent
  max_size_gb    = 2
  zone_redundant = false

  tags = var.tags
}

# Read Model Database
resource "azurerm_mssql_database" "readmodel" {
  name           = "db-${var.project_name}-readmodel-${var.environment}"
  server_id      = module.sql_server.resource_id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  sku_name       = "Basic"
  max_size_gb    = 2
  zone_redundant = false

  tags = var.tags
}

# Azure Verified Module: Log Analytics Workspace
module "log_analytics" {
  source  = "Azure/avm-res-operationalinsights-workspace/azurerm"
  version = "~> 0.4"

  name                = "log-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  log_analytics_workspace_retention_in_days = 30
  log_analytics_workspace_sku               = "PerGB2018"

  tags = var.tags
}

# Azure Verified Module: Application Insights
module "application_insights" {
  source  = "Azure/avm-res-insights-component/azurerm"
  version = "~> 0.1"

  name                = "appi-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  application_type                      = "web"
  workspace_id                          = module.log_analytics.resource_id
  internet_ingestion_enabled            = true
  internet_query_enabled                = true
  local_authentication_disabled         = false

  tags = var.tags
}

# Azure Verified Module: Key Vault
module "key_vault" {
  source  = "Azure/avm-res-keyvault-vault/azurerm"
  version = "~> 0.9"

  name                = "kv-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tenant_id           = var.tenant_id

  sku_name                   = "standard"
  purge_protection_enabled   = false
  soft_delete_retention_days = 7

  network_acls = {
    default_action = var.environment == "dev" ? "Allow" : "Deny"
    bypass         = "AzureServices"
  }

  tags = var.tags
}

# Store connection strings in Key Vault
resource "azurerm_key_vault_secret" "sql_transactional_connection" {
  name         = "sql-transactional-connection"
  value        = "Server=tcp:${module.sql_server.resource.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.transactional.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = module.key_vault.resource_id
}

resource "azurerm_key_vault_secret" "sql_readmodel_connection" {
  name         = "sql-readmodel-connection"
  value        = "Server=tcp:${module.sql_server.resource.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.readmodel.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = module.key_vault.resource_id
}

resource "azurerm_key_vault_secret" "service_bus_connection" {
  name         = "service-bus-connection"
  value        = module.service_bus.resource.default_primary_connection_string
  key_vault_id = module.key_vault.resource_id
}

resource "azurerm_key_vault_secret" "event_grid_access_key" {
  name         = "event-grid-access-key"
  value        = module.event_grid.resource.primary_access_key
  key_vault_id = module.key_vault.resource_id
}

resource "azurerm_key_vault_secret" "app_insights_connection" {
  name         = "app-insights-connection"
  value        = module.application_insights.resource.connection_string
  key_vault_id = module.key_vault.resource_id
}

# Azure Verified Module: Container Apps Environment
module "container_apps_environment" {
  source  = "Azure/avm-res-app-managedenvironment/azurerm"
  version = "~> 0.2"

  name                = "cae-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  log_analytics_workspace_customer_id  = module.log_analytics.resource.workspace_id
  log_analytics_workspace_primary_shared_key = module.log_analytics.resource.primary_shared_key

  tags = var.tags
}

# Container Apps (using custom module as we need specific configurations)
module "container_apps" {
  source = "./modules/container-apps"

  resource_group_name         = azurerm_resource_group.main.name
  location                    = azurerm_resource_group.main.location
  container_app_environment_id = module.container_apps_environment.resource_id
  project_name                = var.project_name
  environment                 = var.environment
  key_vault_id                = module.key_vault.resource_id

  apps = {
    prospect-service = {
      name     = "prospect-service"
      image    = "mcr.microsoft.com/dotnet/samples:aspnetapp" # Placeholder
      port     = 5000
      cpu      = 0.25
      memory   = "0.5Gi"
      min_replicas = 1
      max_replicas = 3
    }
    api-gateway = {
      name     = "api-gateway"
      image    = "mcr.microsoft.com/dotnet/samples:aspnetapp" # Placeholder
      port     = 5000
      cpu      = 0.5
      memory   = "1Gi"
      min_replicas = 1
      max_replicas = 5
      ingress_enabled = true
      external_ingress = true
    }
    event-relay = {
      name     = "event-relay"
      image    = "mcr.microsoft.com/dotnet/samples:aspnetapp" # Placeholder
      port     = 5000
      cpu      = 0.25
      memory   = "0.5Gi"
      min_replicas = 1
      max_replicas = 1
    }
    projection-service = {
      name     = "projection-service"
      image    = "mcr.microsoft.com/dotnet/samples:aspnetapp" # Placeholder
      port     = 5000
      cpu      = 0.25
      memory   = "0.5Gi"
      min_replicas = 1
      max_replicas = 3
    }
  }

  tags = var.tags
}
