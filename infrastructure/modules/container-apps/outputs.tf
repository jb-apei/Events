output "container_app_ids" {
  description = "Map of container app names to resource IDs"
  value = {
    for k, v in azurerm_container_app.apps : k => v.id
  }
}

output "container_app_identities" {
  description = "Map of container app names to managed identity principal IDs"
  value = {
    for k, v in azurerm_container_app.apps : k => {
      principal_id = v.identity[0].principal_id
      tenant_id    = v.identity[0].tenant_id
    }
  }
}

output "container_app_fqdns" {
  description = "Map of container app names to FQDNs"
  value = {
    for k, v in azurerm_container_app.apps : k => try(v.ingress[0].fqdn, null)
  }
}

output "api_gateway_fqdn" {
  description = "API Gateway FQDN"
  value       = try(azurerm_container_app.apps["api-gateway"].ingress[0].fqdn, null)
}

output "api_gateway_url" {
  description = "API Gateway URL"
  value       = try("https://${azurerm_container_app.apps["api-gateway"].ingress[0].fqdn}", null)
}

output "frontend_fqdn" {
  description = "Frontend FQDN"
  value       = try(azurerm_container_app.apps["frontend"].ingress[0].fqdn, null)
}

output "frontend_url" {
  description = "Frontend URL"
  value       = try("https://${azurerm_container_app.apps["frontend"].ingress[0].fqdn}", null)
}
