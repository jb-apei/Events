# Custom wrapper for Container Apps with specific configurations

resource "azurerm_container_app" "apps" {
  for_each = var.apps

  name                         = "ca-${var.project_name}-${each.value.name}-${var.environment}"
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  # Configure registry with managed identity authentication
  dynamic "registry" {
    for_each = var.acr_login_server != "" ? [1] : []

    content {
      server   = var.acr_login_server
      identity = "system"
    }
  }

  template {
    container {
      name   = each.value.name
      image  = each.value.image
      cpu    = each.value.cpu
      memory = each.value.memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = title(var.environment)
      }

      env {
        name  = "AZURE_KEY_VAULT_ENDPOINT"
        value = var.key_vault_id
      }

      # Add custom environment variables per app
      dynamic "env" {
        for_each = try(each.value.env_vars, {})
        content {
          name  = env.key
          value = env.value
        }
      }

      # Liveness probe - restart container if unhealthy
      dynamic "liveness_probe" {
        for_each = try(each.value.health_check_path, null) != null ? [1] : []
        content {
          transport = "HTTP"
          port      = each.value.port
          path      = try(each.value.health_check_path, "/health")
          interval_seconds       = 30
          timeout                = 5
          failure_count_threshold = 3
          initial_delay          = 10
        }
      }

      # Readiness probe - remove from load balancer if unhealthy
      dynamic "readiness_probe" {
        for_each = try(each.value.health_check_path, null) != null ? [1] : []
        content {
          transport = "HTTP"
          port      = each.value.port
          path      = try(each.value.health_check_path, "/health")
          interval_seconds       = 10
          timeout                = 3
          failure_count_threshold = 3
          success_count_threshold = 1
        }
      }

      # Startup probe - wait for container initialization
      dynamic "startup_probe" {
        for_each = try(each.value.health_check_path, null) != null ? [1] : []
        content {
          transport = "HTTP"
          port      = each.value.port
          path      = try(each.value.health_check_path, "/health")
          interval_seconds       = 3
          timeout                = 3
          failure_count_threshold = 10
        }
      }
    }

    min_replicas = each.value.min_replicas
    max_replicas = each.value.max_replicas
  }

  dynamic "ingress" {
    for_each = try(each.value.ingress_enabled, false) ? [1] : []

    content {
      allow_insecure_connections = false
      external_enabled           = try(each.value.external_ingress, false)
      target_port                = each.value.port

      traffic_weight {
        latest_revision = true
        percentage      = 100
      }
    }
  }

  tags = var.tags
}

# Grant Key Vault access to Container Apps managed identities
resource "azurerm_key_vault_access_policy" "container_apps" {
  for_each = var.apps

  key_vault_id = var.key_vault_id
  tenant_id    = azurerm_container_app.apps[each.key].identity[0].tenant_id
  object_id    = azurerm_container_app.apps[each.key].identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}
