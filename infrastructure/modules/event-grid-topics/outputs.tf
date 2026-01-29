output "topic_ids" {
  description = "Map of topic names to resource IDs"
  value = {
    for k, v in azurerm_eventgrid_topic.topics : k => v.id
  }
}

output "topic_endpoints" {
  description = "Map of topic names to endpoints"
  value = {
    for k, v in azurerm_eventgrid_topic.topics : k => v.endpoint
  }
}

output "topic_access_keys" {
  description = "Map of topic names to primary access keys"
  value = {
    for k, v in azurerm_eventgrid_topic.topics : k => v.primary_access_key
  }
  sensitive = true
}
