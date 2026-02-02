resource "azurerm_eventgrid_event_subscription" "prospect_projection" {
  name  = "sub-projection-service-prospects"
  scope = module.event_grid_topics.topic_ids["prospect-events"]
  event_delivery_schema = "CloudEventSchemaV1_0"

  webhook_endpoint {
    url = "https://${local.projection_service_fqdn}/events/webhook"
    max_events_per_batch = 1
    preferred_batch_size_in_kilobytes = 64
  }
}

resource "azurerm_eventgrid_event_subscription" "student_projection" {
  name  = "sub-projection-service-students"
  scope = module.event_grid_topics.topic_ids["student-events"]
  event_delivery_schema = "CloudEventSchemaV1_0"

  webhook_endpoint {
    url = "https://${local.projection_service_fqdn}/events/webhook"
    max_events_per_batch = 1
    preferred_batch_size_in_kilobytes = 64
  }
}

resource "azurerm_eventgrid_event_subscription" "instructor_projection" {
  name  = "sub-projection-service-instructors"
  scope = module.event_grid_topics.topic_ids["instructor-events"]
  event_delivery_schema = "CloudEventSchemaV1_0"

  webhook_endpoint {
    url = "https://${local.projection_service_fqdn}/events/webhook"
    max_events_per_batch = 1
    preferred_batch_size_in_kilobytes = 64
  }
}
