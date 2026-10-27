resource "azurerm_cognitive_deployment" "chat" {
  name                 = "copilot-chat"
  cognitive_account_id = azurerm_cognitive_account.openai.id

  model {
    format  = "OpenAI"
    name    = var.chat_model_name
    version = var.chat_model_version
  }

  sku {
    name     = "Standard"
    capacity = var.chat_deployment_capacity
  }
}
