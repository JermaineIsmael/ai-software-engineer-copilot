resource "azurerm_user_assigned_identity" "api" {
  name                = "${local.base_name}-${local.suffix}-api-id"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}
