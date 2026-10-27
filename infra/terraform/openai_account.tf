resource "azurerm_cognitive_account" "openai" {
  name                          = "${local.base_name}-${local.suffix}-openai"
  location                      = azurerm_resource_group.main.location
  resource_group_name           = azurerm_resource_group.main.name
  kind                          = "OpenAI"
  sku_name                      = "S0"
  custom_subdomain_name         = "${local.base_name}-${local.suffix}-openai"
  local_auth_enabled            = true
  public_network_access_enabled = true
  tags                          = local.tags
}
