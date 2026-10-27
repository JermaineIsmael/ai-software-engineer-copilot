# Azure Development Infrastructure

This directory contains the Azure DevOps pipeline and Terraform configuration
for a Development deployment. Terraform files are split so each Azure resource
is defined in its own file under `terraform/`.

## Provisioned resources

| Resource | Purpose |
|---|---|
| Resource group | Lifecycle boundary for application resources |
| Virtual network and delegated Container Apps subnet | Internal-only API ingress |
| Container Apps environment | Hosts the API privately inside the VNet |
| Azure Container Registry | Stores the API image; admin credentials are disabled |
| User-assigned managed identity | Pulls the API image and reads Key Vault secrets |
| Key Vault | Stores Azure OpenAI and Azure AI Search keys |
| Azure OpenAI account and deployments | Chat and embedding models |
| Azure AI Search | Hybrid/vector repository search |
| Log Analytics workspace | Container Apps logs |
| Application Insights | Workspace-based monitoring resource |
| Azure Static Web App | Frontend hosting resource, provisioned but not published by this pipeline |

Cosmos DB and Service Bus are intentionally excluded: conversation/workflow
persistence and queue processing are not implemented. Application Insights is
provisioned, but application SDK instrumentation/custom metrics are not yet
integrated.

## Deployment boundary and known gaps

- The API Container App uses **internal ingress only** and the `Production`
  ASP.NET Core environment. `BuildTestExecution:Enabled` is explicitly false.
- The VNet and Container Apps environment are created; this configuration does
  not provision a VPN gateway, peering, or a private developer connection.
  Arrange private VNet access separately before trying to call the API.
- Static Web Apps are provisioned but not deployed. The current frontend calls
  a hard-coded local API URL and the API's CORS policy is localhost-only; the
  frontend/API integration must be updated before publishing the web UI.
- Azure OpenAI and Search endpoints are reachable over their public TLS
  endpoints. Their keys are stored in Key Vault and the API uses a managed
  identity to resolve them. Private endpoints and service firewalls are a
  follow-up hardening step.
- The API is unauthenticated. Internal ingress is a deliberate security
  boundary; do not make it externally reachable until an authentication and
  authorization design is implemented and reviewed.

## One-time prerequisites

1. An Azure subscription and a region that supports the selected Azure OpenAI
   models with sufficient quota. The defaults in `terraform/environments/dev.tfvars`
   are examples; adjust model versions, capacity, and location to what your
   subscription supports.
2. An Azure DevOps project with a service connection named
   `sc-copilot-dev-wif` using **workload identity federation**. Replace the
   name in `azure-pipelines.yml` if yours differs.
3. Configure least-privilege rights for the service connection. It needs
   permissions to create/manage the resources and create scoped role
   assignments (`AcrPull` and `Key Vault Secrets User` for the API identity,
   plus `AcrPush` for the pipeline identity). Use `Contributor` plus
   `Role Based Access Control Administrator` or an equivalent scoped custom
   role at the deployment scope. The service connection also needs
   `Storage Blob Data Contributor` on the separate state account.
4. A pipeline variable group named `copilot-infra-dev` containing:
   - `TF_STATE_RESOURCE_GROUP` — existing resource group for Terraform state
   - `TF_STATE_STORAGE_ACCOUNT` — existing Storage Account for Terraform state
5. A pre-created Blob container named `tfstate` in the state storage account.
   Enable Storage Account versioning and soft delete, deny public Blob access,
   and grant the service connection **Storage Blob Data Contributor** using
   Entra ID. Terraform uses the `azurerm` backend with OIDC and Entra auth.
   The state backend is intentionally a pre-provisioned bootstrap dependency:
   Terraform cannot safely use a remote backend that it has not initialized.
6. Configure an Azure DevOps Environment named `copilot-dev` with an approval
   check. The deployment stage runs only from `main`.
7. Before running the pipeline, make the selected subscription available to
   the service connection. The `subscription_id` value in the sample tfvars is
   a placeholder; the pipeline passes the authenticated subscription ID.

After the first pipeline run creates Key Vault, provision secrets named
`azure-openai-api-key` and `azure-search-api-key` directly into that vault using
the Azure Portal or an authorized administrator session. Do not pass keys as
Terraform variables or Azure DevOps pipeline variables. The second pipeline run
references these versionless Key Vault secret URIs; secret values are never
written to Terraform state or pipeline logs. Grant the operator identity
`Key Vault Secrets Officer` at the vault scope only while creating/updating
those values.

## Pipeline flow

`azure-pipelines.yml` runs `terraform fmt -check`, initializes providers without
the backend, and runs `terraform validate` for PRs and branch updates. On
`main`, after the `copilot-dev` environment approval:

1. Queue with `deployApi=false` (default): Terraform plans and applies the base
   infrastructure. On later runs, it preserves an already deployed API image
   instead of removing the app.
2. An authorized operator stores the OpenAI and Search keys in Key Vault.
3. Queue a new approved run with `deployApi=true`: the hosted agent builds and
   pushes a commit-specific API image to Azure Container Registry; Terraform
   plans and applies the API Container App using that immutable commit tag.

The image is built from the root [Dockerfile](../Dockerfile). The pipeline does
not publish the static frontend or make the private API public.

## Manual Terraform operations

Use the same remote backend and OIDC identity settings as the pipeline. Do not
run `terraform apply` with local state or commit `.tfstate`, `.tfplan`, provider
cache, or secret files.

```powershell
Set-Location infra\terraform
terraform fmt -recursive
terraform init `
  -backend-config="resource_group_name=<state-resource-group>" `
  -backend-config="storage_account_name=<state-storage-account>" `
  -backend-config="container_name=tfstate" `
  -backend-config="key=copilot/dev.tfstate" `
  -backend-config="use_azuread_auth=true" `
  -backend-config="use_oidc=true"
terraform validate
terraform plan -var-file=environments\dev.tfvars -var="subscription_id=<subscription-id>"
```

Only apply from an approved pipeline or an authorized operator session. Review
Azure OpenAI quota and regional availability, Azure AI Search pricing, Container
Apps networking, and resource deletion/soft-delete behavior before applying.
