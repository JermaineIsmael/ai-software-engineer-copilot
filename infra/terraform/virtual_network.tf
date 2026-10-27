resource "azurerm_virtual_network" "main" {
  name                = "${local.base_name}-${local.suffix}-vnet"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  address_space       = ["10.40.0.0/16"]
  tags                = local.tags
}
