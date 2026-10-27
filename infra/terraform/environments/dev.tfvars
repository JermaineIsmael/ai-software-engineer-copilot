environment = "dev"
location    = "eastus2"
name_prefix = "copilot"

# Replace with the subscription selected for the Development deployment.
subscription_id = "<AZURE_SUBSCRIPTION_ID>"

search_sku = "basic"

# Confirm these model versions and regional quota before applying.
chat_model_name               = "gpt-4o-mini"
chat_model_version            = "2024-07-18"
chat_deployment_capacity      = 10
embedding_model_name          = "text-embedding-3-small"
embedding_model_version       = "1"
embedding_deployment_capacity = 1
