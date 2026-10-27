resource "azurerm_search_service" "main" {
  name                          = "${local.base_name}-${local.suffix}-search"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  sku                           = var.search_sku
  replica_count                 = 1
  partition_count               = 1
  public_network_access_enabled = true
  local_authentication_enabled  = true
  authentication_failure_mode   = "http403"
  tags                          = local.tags
}
