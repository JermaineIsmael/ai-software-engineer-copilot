resource "azurerm_container_registry" "main" {
  name                          = "${replace(var.name_prefix, "-", "")}${var.environment}${local.suffix}"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  sku                           = "Basic"
  admin_enabled                 = false
  public_network_access_enabled = true
  tags                          = local.tags
}
