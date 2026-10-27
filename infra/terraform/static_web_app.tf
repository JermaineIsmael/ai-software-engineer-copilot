resource "azurerm_static_web_app" "frontend" {
  name                = "${local.base_name}-${local.suffix}-swa"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = local.tags
}
