variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "container_app_environment_id" {
  description = "Container Apps Environment ID"
  type        = string
}

variable "project_name" {
  description = "Project name"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "key_vault_id" {
  description = "Key Vault resource ID"
  type        = string
}

variable "apps" {
  description = "Map of container apps to create"
  type = map(object({
    name             = string
    image            = string
    port             = number
    cpu              = number
    memory           = string
    min_replicas     = number
    max_replicas     = number
    ingress_enabled  = optional(bool, false)
    external_ingress = optional(bool, false)
  }))
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
