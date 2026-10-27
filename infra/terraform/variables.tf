variable "subscription_id" {
  description = "Azure subscription ID for the deployment."
  type        = string
}

variable "location" {
  description = "Azure region. Confirm Azure OpenAI model availability and quota here."
  type        = string
  default     = "eastus2"
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "dev"

  validation {
    condition     = var.environment == "dev"
    error_message = "This baseline is intentionally limited to the dev environment."
  }
}

variable "name_prefix" {
  description = "Short lowercase prefix used for Azure resource names."
  type        = string
  default     = "copilot"

  validation {
    condition     = can(regex("^[a-z0-9-]{3,10}$", var.name_prefix))
    error_message = "name_prefix must contain 3-10 lowercase letters, digits, or hyphens."
  }
}

variable "tags" {
  description = "Additional resource tags."
  type        = map(string)
  default     = {}
}

variable "chat_model_name" {
  description = "Azure OpenAI chat model name."
  type        = string
  default     = "gpt-4o-mini"
}

variable "chat_model_version" {
  description = "Azure OpenAI chat model version supported in the selected region."
  type        = string
  default     = "2024-07-18"
}

variable "chat_deployment_capacity" {
  description = "Azure OpenAI chat deployment capacity in model-specific units."
  type        = number
  default     = 10

  validation {
    condition     = var.chat_deployment_capacity >= 1
    error_message = "chat_deployment_capacity must be at least 1."
  }
}

variable "embedding_model_name" {
  description = "Azure OpenAI embedding model name."
  type        = string
  default     = "text-embedding-3-small"
}

variable "embedding_model_version" {
  description = "Azure OpenAI embedding model version supported in the selected region."
  type        = string
  default     = "1"
}

variable "embedding_deployment_capacity" {
  description = "Azure OpenAI embedding deployment capacity in model-specific units."
  type        = number
  default     = 1

  validation {
    condition     = var.embedding_deployment_capacity >= 1
    error_message = "embedding_deployment_capacity must be at least 1."
  }
}

variable "search_sku" {
  description = "Azure AI Search tier. Basic is the starting development tier."
  type        = string
  default     = "basic"

  validation {
    condition     = contains(["basic", "standard"], var.search_sku)
    error_message = "search_sku must be basic or standard."
  }
}

variable "api_image" {
  description = "Fully qualified API container image, including a unique tag."
  type        = string
  default     = ""
}

variable "deploy_api" {
  description = "Create the API Container App after the image is built and Key Vault secrets are provisioned."
  type        = bool
  default     = false
}

variable "api_min_replicas" {
  description = "Minimum API replicas; zero allows scale-to-zero in development."
  type        = number
  default     = 0
}

variable "api_max_replicas" {
  description = "Maximum API replicas."
  type        = number
  default     = 2
}

variable "api_cpu" {
  description = "CPU allocation for the API container."
  type        = number
  default     = 0.5
}

variable "api_memory" {
  description = "Memory allocation for the API container."
  type        = string
  default     = "1Gi"
}
