# Custom wrapper for Event Grid Topics (until AVM has dedicated module)

resource "azurerm_eventgrid_topic" "topics" {
  for_each = toset(var.topics)

  name                = "evgt-${var.project_name}-${each.key}-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name

  input_schema         = "CloudEventSchemaV1_0"
  public_network_access_enabled = var.public_network_access_enabled

  tags = var.tags
}

# Create Event Grid System Topic for webhook subscriptions
resource "azurerm_eventgrid_system_topic" "webhook" {
  for_each = toset(var.topics)

  name                   = "evgst-${var.project_name}-${each.key}-${var.environment}"
  resource_group_name    = var.resource_group_name
  location               = var.location
  source_arm_resource_id = azurerm_eventgrid_topic.topics[each.key].id
  topic_type             = "Microsoft.EventGrid.Topics"

  tags = var.tags
}
