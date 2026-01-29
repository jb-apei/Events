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
