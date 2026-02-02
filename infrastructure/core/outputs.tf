output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "location" {
  description = "Azure region"
  value       = azurerm_resource_group.main.location
}

output "service_bus_namespace" {
  description = "Service Bus namespace name"
  value       = module.service_bus.resource.name
  sensitive   = true
}

output "service_bus_connection_string" {
  description = "Service Bus connection string (sensitive)"
  value       = module.service_bus.resource.default_primary_connection_string
  sensitive   = true
}

output "event_grid_domain_endpoint" {
  description = "Event Grid domain endpoint"
  value       = module.event_grid.domain_endpoint
}

output "event_grid_topics" {
  description = "Event Grid topic endpoints"
  value       = module.event_grid_topics.topic_endpoints
}

output "event_grid_topic_ids" {
  description = "Event Grid topic IDs for subscriptions"
  value       = module.event_grid_topics.topic_ids
}

output "event_grid_topic_access_keys" {
  description = "Event Grid topic access keys"
  value       = module.event_grid_topics.topic_access_keys
  sensitive   = true
}

output "sql_server_fqdn" {
  description = "SQL Server fully qualified domain name"
  value       = module.sql_server.resource.fully_qualified_domain_name
  sensitive   = true
}

output "transactional_database_name" {
  description = "Transactional database name"
  value       = azurerm_mssql_database.transactional.name
}

output "readmodel_database_name" {
  description = "Read model database name"
  value       = azurerm_mssql_database.readmodel.name
}

output "container_registry_login_server" {
  description = "Container Registry login server URL"
  value       = module.container_registry.resource.login_server
}

output "container_registry_id" {
  description = "Container Registry Resource ID"
  value       = module.container_registry.resource_id
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = module.key_vault.resource_id
}

output "key_vault_id" {
    description = "Key Vault ID"
    value       = module.key_vault.resource_id
}

output "key_vault_uri_value" {
  description = "Key Vault URI Value"
  value       = module.key_vault.uri
}

output "application_insights_connection_string" {
  description = "Application Insights connection string (sensitive)"
  value       = module.application_insights.resource.connection_string
  sensitive   = true
}

output "container_apps_environment_id" {
  description = "Container Apps Environment ID"
  value       = module.container_apps_environment.resource_id
}

output "container_apps_default_domain" {
  description = "Container Apps Default Domain"
  value       = module.container_apps_environment.default_domain
}

output "jwt_secret" {
  description = "JWT Secret"
  value       = random_password.jwt_secret.result
  sensitive   = true
}
