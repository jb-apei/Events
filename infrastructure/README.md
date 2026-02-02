# Infrastructure - Terraform Configuration

This directory contains Terraform configuration for provisioning Azure resources using Azure Verified Modules (AVM) where available.

## Current Deployment

**Environment**: Development (`dev`)  
**Resource Group**: `rg-events-dev`  
**Location**: `westus2`  
**Terraform Version**: >= 1.11 (required for AVM module compatibility)

## Architecture

**Core Services:**
- **Service Bus**: Commands queue (`identity-commands`) with DLQ
- **Event Grid**: 3 topics for domain events (`prospect-events`, `student-events`, `instructor-events`)
- **Azure SQL**: Transactional and read model databases (Basic tier, 5 DTU)
- **Container Apps**: 7 microservices with auto-scaling
- **Container Apps Environment**: Shared environment for all services

**Security & Monitoring:**
- **Key Vault**: Secrets for connection strings and App Insights keys
- **Application Insights**: Distributed tracing and monitoring
- **Log Analytics**: Centralized logging for all services
- **Managed Identities**: Each container app has identity for Key Vault and ACR access

**Container Registry:**
- **Azure Container Registry**: `acreventsdevrcwv3i` (Premium tier for performance)

## Azure Verified Modules Used

- `Azure/avm-res-servicebus-namespace/azurerm` (0.4.0) - Service Bus with queues and topics
- `Azure/avm-res-eventgrid-domain/azurerm` (0.1.0) - Event Grid domain
- `Azure/avm-res-sql-server/azurerm` (0.1.6) - SQL Server with databases
- `Azure/avm-res-operationalinsights-workspace/azurerm` (0.5.1) - Log Analytics
- `Azure/avm-res-insights-component/azurerm` (0.2.0) - Application Insights
- `Azure/avm-res-keyvault-vault/azurerm` (0.10.2) - Key Vault
- `Azure/avm-res-app-managedenvironment/azurerm` (0.4.0) - Container Apps Environment
- `Azure/avm-res-containerregistry-registry/azurerm` (0.5.1) - Container Registry

## Custom Modules

- `modules/container-apps` - Container Apps with Key Vault integration, managed identities, and ACR pull permissions
- `modules/event-grid-topics` - Event Grid Topics wrapper (until AVM provides official module)

## Prerequisites

1. **Terraform** >= 1.11 (required for AVM elasticpool module)
2. **Azure CLI** logged in (`az login`)
3. **Azure Subscription** with Contributor role
4. **PowerShell 7+** (for deployment scripts)

## Container Apps Deployed

All 7 services deployed to Azure Container Apps Environment:

1. **api-gateway** - REST API + WebSocket hub (external ingress)
2. **prospect-service** - Prospect write model (internal only)
3. **student-service** - Student write model (internal only)
4. **instructor-service** - Instructor write model (internal only)
5. **event-relay** - Outbox publisher (internal only)
6. **projection-service** - Read model updates (internal only)
7. **frontend** - React UI (external ingress)

## Setup

1. Copy the example variables file:
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```

2. Edit `terraform.tfvars` with your values:
   - `subscription_id` - Your Azure subscription ID
   - `tenant_id` - Your Azure AD tenant ID  
   - `sql_admin_password` - Strong password for SQL Server (16+ chars, complex)
   - `environment` - Environment name (e.g., "dev", "test", "prod")
   - `location` - Azure region (default: "westus2")

3. Initialize Terraform:
   ```bash
   terraform init
   ```

   This downloads all AVM modules and configures the backend.

4. Review the execution plan:
   ```bash
   terraform plan -out=tfplan
   ```

   Review all resources that will be created.

5. Apply the configuration:
   ```bash
   terraform apply tfplan
   ```

   Or use auto-approve for non-interactive:
   ```bash
   terraform apply -auto-approve
   ```

## Outputs

After successful deployment, Terraform outputs critical configuration:

**Public URLs:**
- `api_gateway_url` - API Gateway endpoint (https://ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io)
- `frontend_url` - Frontend endpoint (https://ca-events-frontend-dev.orangehill-95ada862.eastus2.azurecontainerapps.io)

**Infrastructure IDs:**
- `container_apps_environment_id` - Container Apps Environment resource ID
- `resource_group_name` - Resource group name (`rg-events-dev`)
- `container_registry_name` - ACR registry name (`acreventsdevrcwv3i`)
- `container_registry_login_server` - ACR login server URL
- `key_vault_uri` - Key Vault URI for secrets

**Database:**
- `sql_server_fqdn` - SQL Server FQDN (sensitive)
- `transactional_database_name` - Transactional DB name
- `readmodel_database_name` - Read model DB name

**Messaging:**
- `service_bus_namespace` - Service Bus namespace (sensitive)
- `service_bus_connection_string` - Connection string (sensitive)
- `event_grid_topics` - Map of Event Grid topic endpoints

**Monitoring:**
- `application_insights_connection_string` - App Insights connection (sensitive)
- `application_insights_instrumentation_key` - Instrumentation key (sensitive)

View outputs:
```bash
terraform output

# View sensitive outputs
terraform output -json | jq '.sql_server_fqdn.value'
```

## Environments

The current setup uses a single development environment. For multiple environments, use Terraform workspaces or separate tfvars files:

**Option 1: Terraform Workspaces**
```bash
# Create and switch to test workspace
terraform workspace new test
terraform apply -var="environment=test"

# Create and switch to prod workspace
terraform workspace new prod
terraform apply -var="environment=prod"

# List workspaces
terraform workspace list

# Switch between workspaces
terraform workspace select dev
```

**Option 2: Separate tfvars Files**
```bash
# Create environment-specific tfvars
cp terraform.tfvars terraform.tfvars.test
cp terraform.tfvars terraform.tfvars.prod

# Apply with specific tfvars
terraform apply -var-file="terraform.tfvars.test"
terraform apply -var-file="terraform.tfvars.prod"
```

**Resource Naming Convention:**
- Resources include environment suffix (e.g., `-dev`, `-test`, `-prod`)
- Ensures isolation between environments
- Example: `ca-events-api-gateway-dev`, `ca-events-api-gateway-prod`

## State Management

**Current**: Local state file (`terraform.tfstate`)

**Recommended for Production**: Remote state in Azure Storage

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstateevents"
    container_name       = "tfstate"
    key                  = "events-dev.tfstate"
  }
}
```

Benefits of remote state:
- Team collaboration (shared state)
- State locking (prevents concurrent modifications)
- Encryption at rest
- Versioning and history

## Troubleshooting

### Terraform Version Mismatch

**Error**: `Unsupported Terraform Core version`

**Solution**:
```bash
# Remove cached modules
rm -rf .terraform

# Reinitialize with latest modules
terraform init -upgrade
```

Ensure Terraform version >= 1.11:
```bash
terraform --version
```

### State Lock Issues

**Error**: `Error acquiring the state lock`

**Solution**:
```bash
# Force unlock (use with caution)
terraform force-unlock <lock-id>
```

### Module Download Failures

**Error**: Failed to download modules from registry

**Solution**:
```bash
# Clear module cache
rm -rf .terraform/modules

# Retry initialization
terraform init -upgrade
```

### Azure Authentication Expired

**Error**: Token expired or authentication failed

**Solution**:
```bash
# Re-authenticate
az login

# Refresh token
az account get-access-token
```

## Resource Dependencies

Key dependencies managed by Terraform:

1. **Resource Group** created first
2. **Log Analytics + App Insights** before Container Apps
3. **Key Vault** before storing secrets
4. **SQL Server** before databases
5. **Container Apps Environment** before individual apps
6. **ACR** before container app deployments
7. **Managed Identities** assigned to apps before Key Vault policies
8. **Role Assignments** for ACR pull and Key Vault access

## Cost Estimate

**Development Environment (per month)**:
- Container Apps: ~$50 (based on usage)
- Azure SQL (Basic tier): ~$5
- Service Bus (Basic): ~$0.05
- Event Grid: ~$1 (based on events)
- Key Vault: ~$0.03
- Application Insights: ~$2 (basic tier)
- Container Registry (Premium): ~$40
- **Total**: ~$100/month

**Note**: Actual costs vary based on usage patterns. Container Apps scales to zero when idle.
    container_name       = "tfstate"
    key                  = "events.tfstate"
  }
}
```

## Cost Optimization

- **Dev Environment**: ~$50-100/month
  - Service Bus Standard
  - SQL Basic tier (2 databases)
  - Container Apps (small instances)
  
- **Prod Environment**: Scale up as needed
  - Service Bus Premium for better performance
  - SQL Standard tier with auto-scaling
  - Container Apps with more replicas

## Security Notes

1. **Never commit `terraform.tfvars`** - It contains sensitive data
2. **Use Azure Key Vault** - All secrets stored securely
3. **Managed Identity** - Container Apps use MI for Key Vault access
4. **Network Security** - Private endpoints for prod environments
5. **TLS 1.2+** - Enforced on SQL Server

## Troubleshooting

### Authentication Issues
```bash
az login
az account set --subscription <subscription-id>
```

### State Lock
```bash
terraform force-unlock <lock-id>
```

### Module Not Found
```bash
terraform init -upgrade
```

## Cleanup

To destroy all resources:

```bash
terraform destroy
```

**Warning**: This will delete all data. Use with caution!
