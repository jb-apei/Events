terraform {
  required_version = ">= 1.11"

  backend "azurerm" {
    resource_group_name  = "rg-events-tfstate"
    storage_account_name = "steventsstate8712"
    container_name       = "tfstate"
    key                  = "apps.tfstate"
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.58"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

data "terraform_remote_state" "core" {
  backend = "azurerm"
  config = {
    resource_group_name  = "rg-events-tfstate"
    storage_account_name = "steventsstate8712"
    container_name       = "tfstate"
    key                  = "core.tfstate"
  }
}

locals {
  container_app_base_domain = data.terraform_remote_state.core.outputs.container_apps_default_domain

  # Construct FQDNs for services
  api_gateway_fqdn = "ca-${var.project_name}-api-gateway-${var.environment}.${local.container_app_base_domain}"
  api_gateway_url  = "https://${local.api_gateway_fqdn}"

  prospect_service_fqdn   = "ca-${var.project_name}-prospect-service-${var.environment}.internal.${local.container_app_base_domain}"
  student_service_fqdn    = "ca-${var.project_name}-student-service-${var.environment}.internal.${local.container_app_base_domain}"
  instructor_service_fqdn = "ca-${var.project_name}-instructor-service-${var.environment}.internal.${local.container_app_base_domain}"
  projection_service_fqdn = "ca-${var.project_name}-projection-service-${var.environment}.${local.container_app_base_domain}"
}

# Container Apps
module "container_apps" {
  source = "../modules/container-apps"

  resource_group_name          = data.terraform_remote_state.core.outputs.resource_group_name
  location                     = data.terraform_remote_state.core.outputs.location
  container_app_environment_id = data.terraform_remote_state.core.outputs.container_apps_environment_id
  project_name                 = var.project_name
  environment                  = var.environment
  key_vault_id                 = data.terraform_remote_state.core.outputs.key_vault_id
  key_vault_uri                = data.terraform_remote_state.core.outputs.key_vault_uri_value
  acr_login_server             = data.terraform_remote_state.core.outputs.container_registry_login_server

  apps = {
    prospect-service = {
      name              = "prospect-service"
      image             = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/prospect-service:${var.image_tag}"
      acr_resource_id   = data.terraform_remote_state.core.outputs.container_registry_id
      port              = 8080
      cpu               = 0.25
      memory            = "0.5Gi"
      min_replicas      = 1
      max_replicas      = 3
      ingress_enabled   = true
      external_ingress  = false
      health_check_path = "/health"
      env_vars = {
        ConnectionStrings__ProspectDb       = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.transactional_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        ServiceBus__ConnectionString        = data.terraform_remote_state.core.outputs.service_bus_connection_string
        Azure__ServiceBus__ConnectionString = data.terraform_remote_state.core.outputs.service_bus_connection_string
        ApiGateway__Url                     = local.api_gateway_url
        ApiGateway__PushEvents              = "true"
      }
    }
    student-service = {
      name              = "student-service"
      image             = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/student-service:${var.image_tag}"
      acr_resource_id   = data.terraform_remote_state.core.outputs.container_registry_id
      port              = 8080
      cpu               = 0.25
      memory            = "0.5Gi"
      min_replicas      = 1
      max_replicas      = 3
      ingress_enabled   = true
      external_ingress  = false
      health_check_path = "/health"
      env_vars = {
        ConnectionStrings__StudentDb        = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.transactional_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        ServiceBus__ConnectionString        = data.terraform_remote_state.core.outputs.service_bus_connection_string
        Azure__ServiceBus__ConnectionString = data.terraform_remote_state.core.outputs.service_bus_connection_string
        ApiGateway__Url                     = local.api_gateway_url
        ApiGateway__PushEvents              = "true"
      }
    }
    instructor-service = {
      name              = "instructor-service"
      image             = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/instructor-service:${var.image_tag}"
      acr_resource_id   = data.terraform_remote_state.core.outputs.container_registry_id
      port              = 8080
      cpu               = 0.25
      memory            = "0.5Gi"
      min_replicas      = 1
      max_replicas      = 3
      ingress_enabled   = true
      external_ingress  = false
      health_check_path = "/health"
      env_vars = {
        ConnectionStrings__InstructorDb     = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.transactional_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        ServiceBus__ConnectionString        = data.terraform_remote_state.core.outputs.service_bus_connection_string
        Azure__ServiceBus__ConnectionString = data.terraform_remote_state.core.outputs.service_bus_connection_string
        ApiGateway__Url                     = local.api_gateway_url
        ApiGateway__PushEvents              = "true"
      }
    }
    api-gateway = {
      name              = "api-gateway"
      image             = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/api-gateway:${var.image_tag}"
      acr_resource_id   = data.terraform_remote_state.core.outputs.container_registry_id
      port              = 8080
      cpu               = 1.0
      memory            = "2Gi"
      min_replicas      = 2
      max_replicas      = 10
      ingress_enabled   = true
      external_ingress  = true
      health_check_path = "/health"
      env_vars = {
        ConnectionStrings__ReadModelDb        = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.readmodel_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        ConnectionStrings__ProspectDb         = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.transactional_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        ServiceBus__ConnectionString          = data.terraform_remote_state.core.outputs.service_bus_connection_string
        ApplicationInsights__ConnectionString = data.terraform_remote_state.core.outputs.application_insights_connection_string
        Jwt__SecretKey                        = data.terraform_remote_state.core.outputs.jwt_secret
        StudentService__Url                   = "https://${local.student_service_fqdn}"
        InstructorService__Url                = "https://${local.instructor_service_fqdn}"
        ProspectService__Url                  = "https://${local.prospect_service_fqdn}"
      }
    }
    event-relay = {
      name              = "event-relay"
      image             = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/event-relay:${var.image_tag}"
      acr_resource_id   = data.terraform_remote_state.core.outputs.container_registry_id
      port              = 8080
      cpu               = 0.25
      memory            = "0.5Gi"
      min_replicas      = 1
      max_replicas      = 1
      env_vars = {
        ConnectionStrings__ProspectDb             = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.transactional_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        Azure__EventGrid__ProspectTopicEndpoint   = data.terraform_remote_state.core.outputs.event_grid_topics["prospect-events"]
        Azure__EventGrid__ProspectTopicKey        = data.terraform_remote_state.core.outputs.event_grid_topic_access_keys["prospect-events"]
        Azure__EventGrid__StudentTopicEndpoint    = data.terraform_remote_state.core.outputs.event_grid_topics["student-events"]
        Azure__EventGrid__StudentTopicKey         = data.terraform_remote_state.core.outputs.event_grid_topic_access_keys["student-events"]
        Azure__EventGrid__InstructorTopicEndpoint = data.terraform_remote_state.core.outputs.event_grid_topics["instructor-events"]
        Azure__EventGrid__InstructorTopicKey      = data.terraform_remote_state.core.outputs.event_grid_topic_access_keys["instructor-events"]
      }
    }
    projection-service = {
      name              = "projection-service"
      image             = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/projection-service:${var.image_tag}"
      acr_resource_id   = data.terraform_remote_state.core.outputs.container_registry_id
      port              = 8080
      cpu               = 0.25
      memory            = "0.5Gi"
      min_replicas      = 1
      max_replicas      = 3
      ingress_enabled   = true
      external_ingress  = true
      health_check_path = "/health"
      env_vars = {
        ConnectionStrings__ProjectionDatabase = "Server=tcp:${data.terraform_remote_state.core.outputs.sql_server_fqdn},1433;Initial Catalog=${data.terraform_remote_state.core.outputs.readmodel_database_name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        ServiceBus__ConnectionString          = data.terraform_remote_state.core.outputs.service_bus_connection_string
        Azure__ServiceBus__ConnectionString   = data.terraform_remote_state.core.outputs.service_bus_connection_string
        ApiGateway__Url                       = local.api_gateway_url
      }
    }
    frontend = {
      name             = "frontend"
      image            = "${data.terraform_remote_state.core.outputs.container_registry_login_server}/frontend:${var.image_tag}"
      acr_resource_id  = data.terraform_remote_state.core.outputs.container_registry_id
      port             = 80
      cpu              = 0.25
      memory           = "0.5Gi"
      min_replicas     = 1
      max_replicas     = 3
      ingress_enabled  = true
      external_ingress = true
      env_vars = {
        API_GATEWAY_URL = local.api_gateway_url
      }
    }
  }

  tags = var.tags
}

# Event Grid Event Subscriptions to ProjectionService webhook
resource "azurerm_eventgrid_event_subscription" "projection_subscriptions" {
  for_each = data.terraform_remote_state.core.outputs.event_grid_topic_ids

  name                  = "sub-${each.key}-to-projection"
  scope                 = each.value
  event_delivery_schema = "CloudEventSchemaV1_0"

  webhook_endpoint {
    url = "https://${module.container_apps.container_app_fqdns["projection-service"]}/events/webhook"
  }

  included_event_types = [
    "ProspectCreated",
    "ProspectUpdated",
    "ProspectMerged",
    "StudentCreated",
    "StudentUpdated",
    "StudentChanged",
    "InstructorCreated",
    "InstructorUpdated",
    "InstructorDeactivated"
  ]

  retry_policy {
    max_delivery_attempts = 30
    event_time_to_live    = 1440 # 24 hours
  }

  depends_on = [module.container_apps]
}

# Grant Container Apps AcrPull permission
resource "azurerm_role_assignment" "container_apps_acr_pull" {
  for_each = module.container_apps.container_app_identities

  scope                = data.terraform_remote_state.core.outputs.container_registry_id
  role_definition_name = "AcrPull"
  principal_id         = each.value.principal_id

  depends_on = [module.container_apps]
}
