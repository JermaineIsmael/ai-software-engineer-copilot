output "resource_group_name" {
  description = "Name of the application resource group."
  value       = azurerm_resource_group.main.name
}

output "container_registry_name" {
  description = "Name of the API image registry."
  value       = azurerm_container_registry.main.name
}

output "container_registry_login_server" {
  description = "Login server used to tag and push the API image."
  value       = azurerm_container_registry.main.login_server
}

output "key_vault_name" {
  description = "Key Vault name; API-key secrets must be provisioned there."
  value       = azurerm_key_vault.main.name
}

output "openai_endpoint" {
  description = "Azure OpenAI endpoint."
  value       = azurerm_cognitive_account.openai.endpoint
}

output "search_endpoint" {
  description = "Azure AI Search endpoint."
  value       = "https://${azurerm_search_service.main.name}.search.windows.net"
}

output "static_web_app_name" {
  description = "Static Web App resource name. Deployment remains separate."
  value       = azurerm_static_web_app.frontend.name
}

output "container_app_name" {
  description = "API Container App name when deploy_api is enabled."
  value       = try(azurerm_container_app.api[0].name, null)
}

output "container_app_image" {
  description = "Current API container image, when the API is deployed."
  value       = try(azurerm_container_app.api[0].template[0].container[0].image, null)
}

output "container_apps_environment_id" {
  description = "Internal Container Apps environment ID."
  value       = azurerm_container_app_environment.main.id
}
