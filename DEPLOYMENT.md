# Deployment Guide

This guide covers deploying the Events system to Azure Container Apps.

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- Terraform installed (v1.6+)
- PowerShell 7+ (for deployment script)
- Access to Azure subscription with appropriate permissions

## Quick Deploy

The simplest way to deploy is using the automated deployment script:

```powershell
.\deploy.ps1
```

This will:
1. Build all container images in Azure ACR
2. Apply Terraform infrastructure changes
3. Restart container apps with new images
4. Verify deployment status

## Deployment Options

### Full Deployment (First Time)

```powershell
.\deploy.ps1
```

### Update Code Only (Skip Infrastructure)

If you only changed application code and infrastructure is unchanged:

```powershell
.\deploy.ps1 -SkipTerraform
```

### Infrastructure Changes Only

If you only updated Terraform configuration:

```powershell
.\deploy.ps1 -SkipBuild -SkipRestart
```

### Using Existing Images

If images are already built and you just want to update infrastructure:

```powershell
.\deploy.ps1 -SkipBuild
```

## Manual Deployment Steps

If you prefer manual control:

### 1. Build Container Images

```powershell
.\build-and-push-acr.ps1 -RegistryName acreventsdevrcwv3i
```

Or build individual services:

```powershell
az acr build --registry acreventsdevrcwv3i `
  --image api-gateway:latest `
  -f src/services/ApiGateway/Dockerfile `
  src/services
```

### 2. Apply Infrastructure Changes

```powershell
cd infrastructure
terraform plan -out=tfplan
terraform apply tfplan
```

### 3. Restart Container Apps

```powershell
az containerapp revision restart `
  --name ca-events-api-gateway-dev `
  --resource-group rg-events-dev `
  --revision $(az containerapp revision list `
    --name ca-events-api-gateway-dev `
    --resource-group rg-events-dev `
    --query '[0].name' -o tsv)
```

Repeat for each service:
- `ca-events-prospect-service-dev`
- `ca-events-projection-service-dev`
- `ca-events-event-relay-dev`
- `ca-events-frontend-dev`

### 4. Verify Deployment

```powershell
az containerapp list `
  --resource-group rg-events-dev `
  --query "[].{Name:name, Status:properties.runningStatus, FQDN:properties.configuration.ingress.fqdn}" `
  -o table
```

## CI/CD with GitHub Actions

The project includes a GitHub Actions workflow for automated deployments on push to master.

### Setup GitHub Secrets

Configure these secrets in your GitHub repository:

1. **AZURE_CREDENTIALS**: Service principal credentials (JSON)
   ```json
   {
     "clientId": "<client-id>",
     "clientSecret": "<client-secret>",
     "subscriptionId": "<subscription-id>",
     "tenantId": "<tenant-id>"
   }
   ```

2. **ARM_CLIENT_ID**: Service principal client ID
3. **ARM_CLIENT_SECRET**: Service principal client secret
4. **ARM_SUBSCRIPTION_ID**: Azure subscription ID
5. **ARM_TENANT_ID**: Azure tenant ID

### Create Service Principal

```bash
az ad sp create-for-rbac --name "github-actions-events" \
  --role contributor \
  --scopes /subscriptions/<subscription-id>/resourceGroups/rg-events-dev \
  --sdk-auth
```

Copy the JSON output and add it as the `AZURE_CREDENTIALS` secret.

### Trigger Deployment

Deployments automatically trigger on:
- Push to `master` branch
- Manual workflow dispatch (Actions tab in GitHub)

## Deployment Architecture

### Build Process

All images are built directly in Azure Container Registry using `az acr build`:
- Bypasses local Docker daemon issues
- Faster upload to Azure (no local image push)
- Consistent build environment
- Automatic image tagging

### Infrastructure Updates

Terraform manages:
- Container Apps and Environment
- Event Grid topics and subscriptions
- Service Bus queues and topics
- SQL databases
- Application Insights
- Key Vault secrets
- RBAC permissions

### Container App Updates

Container Apps automatically pull new images when restarted. The deployment process:
1. Builds new images with `latest` tag
2. Applies any infrastructure changes
3. Restarts apps to pick up new images
4. Health checks verify successful deployment

## Monitoring Deployment

### View Build Logs

```powershell
az acr task list-runs --registry acreventsdevrcwv3i --top 10 -o table
```

Get detailed logs for a specific build:

```powershell
az acr task logs --registry acreventsdevrcwv3i --run-id <run-id>
```

### View Container Logs

```powershell
az containerapp logs show `
  --name ca-events-api-gateway-dev `
  --resource-group rg-events-dev `
  --follow
```

### View Application Insights

Navigate to the Azure portal and view the Application Insights resource for real-time telemetry.

## Troubleshooting

### Build Failures

Check ACR build logs:
```powershell
az acr task list-runs --registry acreventsdevrcwv3i --query "[?status=='Failed']" -o table
```

### Container App Not Starting

View logs:
```powershell
az containerapp logs show --name <app-name> --resource-group rg-events-dev
```

Check revision status:
```powershell
az containerapp revision list --name <app-name> --resource-group rg-events-dev -o table
```

### Terraform Errors

Refresh Azure CLI token:
```powershell
az account get-access-token
```

Reinitialize Terraform:
```powershell
cd infrastructure
terraform init -upgrade
```

### ACR Access Issues

Enable admin access (temporary):
```powershell
az acr update --name acreventsdevrcwv3i --admin-enabled true
```

Get credentials:
```powershell
az acr credential show --name acreventsdevrcwv3i
```

## Deployment Checklist

- [ ] Azure CLI authenticated (`az account show`)
- [ ] Terraform state accessible
- [ ] ACR accessible (`az acr check-health -n acreventsdevrcwv3i`)
- [ ] Resource group exists
- [ ] SQL firewall allows Azure services
- [ ] Environment variables configured in Terraform
- [ ] Event Grid webhook endpoints are public
- [ ] Application Insights enabled

## Rollback

To rollback to a previous revision:

```powershell
# List revisions
az containerapp revision list --name ca-events-api-gateway-dev --resource-group rg-events-dev -o table

# Activate previous revision
az containerapp revision activate `
  --name ca-events-api-gateway-dev `
  --resource-group rg-events-dev `
  --revision <previous-revision-name>
```

## URLs After Deployment

Check deployment outputs:

```powershell
cd infrastructure
terraform output
```

Key URLs:
- **Frontend**: Output `frontend_url`
- **API Gateway**: Output `api_gateway_url`
- **Event Grid Topics**: Output `event_grid_topics`
