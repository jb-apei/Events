output "api_gateway_fqdn" {
  description = "API Gateway fully qualified domain name"
  value       = module.container_apps.api_gateway_fqdn
}

output "api_gateway_url" {
  description = "API Gateway public URL"
  value       = module.container_apps.api_gateway_url
}

output "frontend_fqdn" {
  description = "Frontend fully qualified domain name"
  value       = try(module.container_apps.frontend_fqdn, null)
}

output "frontend_url" {
  description = "Frontend public URL"
  value       = try(module.container_apps.frontend_url, null)
}
