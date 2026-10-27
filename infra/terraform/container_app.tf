resource "azurerm_container_app" "api" {
  count = var.deploy_api ? 1 : 0

  name                         = "${local.base_name}-${local.suffix}-api"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"
  tags                         = local.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  registry {
    server   = azurerm_container_registry.main.login_server
    identity = azurerm_user_assigned_identity.api.id
  }

  secret {
    name                = local.key_vault_secret_names.openai_api_key
    key_vault_secret_id = "${azurerm_key_vault.main.vault_uri}secrets/${local.key_vault_secret_names.openai_api_key}"
    identity            = azurerm_user_assigned_identity.api.id
  }

  secret {
    name                = local.key_vault_secret_names.search_api_key
    key_vault_secret_id = "${azurerm_key_vault.main.vault_uri}secrets/${local.key_vault_secret_names.search_api_key}"
    identity            = azurerm_user_assigned_identity.api.id
  }

  template {
    min_replicas = var.api_min_replicas
    max_replicas = var.api_max_replicas

    container {
      name   = "copilot-api"
      image  = var.api_image
      cpu    = var.api_cpu
      memory = var.api_memory

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }

      env {
        name  = "BuildTestExecution__Enabled"
        value = "false"
      }

      env {
        name  = "AzureOpenAI__Endpoint"
        value = azurerm_cognitive_account.openai.endpoint
      }

      env {
        name  = "AzureOpenAI__DeploymentName"
        value = azurerm_cognitive_deployment.chat.name
      }

      env {
        name  = "AzureOpenAI__EmbeddingDeploymentName"
        value = azurerm_cognitive_deployment.embedding.name
      }

      env {
        name        = "AzureOpenAI__ApiKey"
        secret_name = local.key_vault_secret_names.openai_api_key
      }

      env {
        name  = "AzureSearch__Endpoint"
        value = "https://${azurerm_search_service.main.name}.search.windows.net"
      }

      env {
        name  = "AzureSearch__IndexName"
        value = "code-chunks"
      }

      env {
        name        = "AzureSearch__ApiKey"
        secret_name = local.key_vault_secret_names.search_api_key
      }

      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = azurerm_application_insights.main.connection_string
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 8080
    transport        = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  depends_on = [
    azurerm_role_assignment.api_acr_pull,
    azurerm_role_assignment.api_key_vault_secrets_user
  ]

  lifecycle {
    precondition {
      condition     = trimspace(var.api_image) != ""
      error_message = "api_image must be a fully qualified image URI when deploy_api is true."
    }
  }
}
