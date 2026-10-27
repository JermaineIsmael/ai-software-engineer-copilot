locals {
  base_name = "${var.name_prefix}-${var.environment}"
  suffix    = random_string.resource_suffix.result

  tags = merge(var.tags, {
    application = "ai-software-engineer-copilot"
    environment = var.environment
    managed_by  = "terraform"
  })

  key_vault_secret_names = {
    openai_api_key = "azure-openai-api-key"
    search_api_key = "azure-search-api-key"
  }
}
