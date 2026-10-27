resource "azurerm_application_insights" "main" {
  name                 = "${local.base_name}-${local.suffix}-appi"
  location             = azurerm_resource_group.main.location
  resource_group_name  = azurerm_resource_group.main.name
  workspace_id         = azurerm_log_analytics_workspace.main.id
  application_type     = "web"
  retention_in_days    = 30
  daily_data_cap_in_gb = 1
  tags                 = local.tags
}
