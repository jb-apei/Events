# Infrastructure - Terraform Configuration

This directory contains Terraform configuration for provisioning Azure resources using Azure Verified Modules (AVM) where available.

## Architecture

- **Service Bus**: Commands queue with dead-letter handling
- **Event Grid**: Event fan-out with topics for each domain aggregate
- **Azure SQL**: Transactional and read model databases (Basic tier)
- **Container Apps**: Microservices hosting with auto-scaling
- **Key Vault**: Secure storage for connection strings and secrets
- **Application Insights**: Monitoring and distributed tracing
- **Log Analytics**: Centralized logging

## Azure Verified Modules Used

- `Azure/avm-res-servicebus-namespace/azurerm` - Service Bus with queues
- `Azure/avm-res-eventgrid-domain/azurerm` - Event Grid domain
- `Azure/avm-res-sql-server/azurerm` - SQL Server
- `Azure/avm-res-operationalinsights-workspace/azurerm` - Log Analytics
- `Azure/avm-res-insights-component/azurerm` - Application Insights
- `Azure/avm-res-keyvault-vault/azurerm` - Key Vault
- `Azure/avm-res-app-managedenvironment/azurerm` - Container Apps Environment

## Custom Modules

- `modules/event-grid-topics` - Event Grid Topics wrapper (until AVM provides one)
- `modules/container-apps` - Container Apps with Key Vault integration

## Prerequisites

1. **Terraform** >= 1.6
2. **Azure CLI** logged in (`az login`)
3. **Azure Subscription** with appropriate permissions

## Setup

1. Copy the example variables file:
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```

2. Edit `terraform.tfvars` with your values:
   - `subscription_id` - Your Azure subscription ID
   - `tenant_id` - Your Azure AD tenant ID
   - `sql_admin_password` - Strong password for SQL Server

3. Initialize Terraform:
   ```bash
   terraform init
   ```

4. Review the execution plan:
   ```bash
   terraform plan
   ```

5. Apply the configuration:
   ```bash
   terraform apply
   ```

## Outputs

After successful deployment, Terraform outputs:

- **Service Bus connection string** - For command queues
- **Event Grid endpoints** - For event publishing
- **SQL Server FQDN** - For database connections
- **Key Vault URI** - For secret access
- **API Gateway URL** - Public endpoint for React frontend
- **Application Insights keys** - For observability

## Environments

Create separate workspaces for each environment:

```bash
# Development
terraform workspace new dev
terraform apply -var="environment=dev"

# Test
terraform workspace new test
terraform apply -var="environment=test"

# Production
terraform workspace new prod
terraform apply -var="environment=prod"
```

## State Management

For production, use remote state backend:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "sttfstate"
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
