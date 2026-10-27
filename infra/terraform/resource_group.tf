resource "azurerm_resource_group" "main" {
  name     = "${local.base_name}-${local.suffix}-rg"
  location = var.location
  tags     = local.tags
}
