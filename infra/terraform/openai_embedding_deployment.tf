resource "azurerm_cognitive_deployment" "embedding" {
  name                 = "copilot-embedding"
  cognitive_account_id = azurerm_cognitive_account.openai.id

  model {
    format  = "OpenAI"
    name    = var.embedding_model_name
    version = var.embedding_model_version
  }

  sku {
    name     = "Standard"
    capacity = var.embedding_deployment_capacity
  }
}
