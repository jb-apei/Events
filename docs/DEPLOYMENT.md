# Deployment Guide

This guide covers deploying the Events system to Azure Container Apps.

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- Terraform installed (v1.11+)
- PowerShell 7+ (for deployment script)
- Access to Azure subscription with appropriate permissions
- GitHub account (for CI/CD setup)

## Deployment Methods

### 1. Automated Deployment (Recommended)

The simplest way to deploy is using the automated deployment script:

```powershell
# Deploy from local source
.\deploy.ps1

# Deploy from GitHub repository (recommended for production)
.\deploy.ps1 -GitHubRepo "https://github.com/jb-apei/Events.git"
```

This will:
1. ✅ Verify Azure CLI authentication
2. 🏗️ Build all 7 container images in Azure Container Registry
3. 📦 Apply Terraform infrastructure changes
4. 🔄 Restart container apps with new images
5. ✅ Verify deployment status

**Services Deployed:**
- `api-gateway` - REST API + WebSocket hub
- `prospect-service` - Prospect write model
- `student-service` - Student write model
- `instructor-service` - Instructor write model
- `event-relay` - Outbox publisher
- `projection-service` - Read model updates
- `frontend` - React UI

### 2. GitHub Actions (CI/CD)

Automated deployment on every push to `master` branch.

### 2. GitHub Actions (CI/CD)

Automated deployment on every push to `master` branch.

#### Setup GitHub Secrets

**Option A: Automated Setup (Recommended)**
```powershell
.\setup-github-secrets.ps1
```

This script will:
- Create or reset Azure service principal
- Configure GitHub secrets automatically via GitHub CLI
- Set up all 4 required ARM secrets

**Option B: Manual Setup**

1. Create Azure service principal:
```bash
az ad sp create-for-rbac --name "github-actions-events" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

2. Add these secrets to GitHub repository settings:
- `ARM_CLIENT_ID` - Service principal application (client) ID
- `ARM_CLIENT_SECRET` - Service principal password (secret value)
- `ARM_SUBSCRIPTION_ID` - Azure subscription ID
- `ARM_TENANT_ID` - Azure AD tenant ID

See [.github/SECRETS_SETUP.md](.github/SECRETS_SETUP.md) for detailed instructions.

#### Workflow Overview

The GitHub Actions workflow (`.github/workflows/deploy-azure.yml`) includes three jobs:

1. **Build Job**
   - Builds all 7 container images from GitHub source
   - Pushes to Azure Container Registry
   - Uses root context for Docker builds

2. **Terraform Job** (runs in parallel with Build)
   - Applies infrastructure changes
   - Uses service principal authentication (azure/login@v1)
   - Outputs resource identifiers

3. **Restart Job** (runs after Build + Terraform)
   - Restarts all container apps to pull new images
   - Ensures zero-downtime rolling updates

**Trigger Workflow Manually:**
```powershell
gh workflow run deploy-azure.yml --ref master
```

## Deployment Options

### Full Deployment (First Time or After Infrastructure Changes)

```powershell
.\deploy.ps1
```

Performs all steps: Build → Terraform → Restart

### Update Code Only (Skip Infrastructure)

If you only changed application code and infrastructure is unchanged:

```powershell
.\deploy.ps1 -SkipTerraform
```

Performs: Build → Restart

### Infrastructure Changes Only

If you only updated Terraform configuration:

```powershell
.\deploy.ps1 -SkipBuild -SkipRestart
cd infrastructure
terraform plan
terraform apply
```

### Restart Services Only

If images are already built and you just want to restart with existing images:

```powershell
.\deploy.ps1 -SkipBuild -SkipTerraform
```

### Deploy from GitHub Source (Production)

Build images directly from GitHub repository:

```powershell
.\deploy.ps1 -GitHubRepo "https://github.com/jb-apei/Events.git"
```

Benefits:
- Ensures deployed code matches repository
- No local build artifacts
- Consistent builds across environments
- Better for CI/CD pipelines

## Manual Deployment Steps

If you prefer manual control:

### 1. Build Container Images

**Local Build:**
```powershell
.\build-and-push-acr.ps1 -RegistryName acreventsdevrcwv3i
```

**From GitHub Repository:**
```bash
# Build all services from GitHub
az acr build --registry acreventsdevrcwv3i \
  --image api-gateway:latest \
  -f src/services/ApiGateway/Dockerfile \
  https://github.com/jb-apei/Events.git

# Repeat for each service:
# prospect-service, student-service, instructor-service
# event-relay, projection-service, frontend
```

**Build Individual Service:**
```bash
az acr build --registry acreventsdevrcwv3i \
  --image api-gateway:latest \
  -f src/services/ApiGateway/Dockerfile \
  .
```

Note: All Dockerfiles use root context (`.`) and reference `src/services/` for .NET services.

### 2. Apply Infrastructure Changes

```powershell
cd infrastructure

# Review changes
terraform plan -out=tfplan

# Apply changes
terraform apply tfplan
```

**Important Outputs:**
- `api_gateway_url` - API Gateway public URL
- `frontend_url` - Frontend public URL
- `container_registry_login_server` - ACR server name
- `service_bus_connection_string` - Service Bus connection (sensitive)
- `sql_server_fqdn` - SQL Server FQDN (sensitive)

### 3. Restart Container Apps

After building new images, restart services to pull latest:

```bash
# Restart all services
az containerapp list --resource-group rg-events-dev \
  --query "[].name" -o tsv | \
  ForEach-Object { 
    az containerapp update --name $_ --resource-group rg-events-dev
  }
```

**Or restart individual service:**
```bash
az containerapp update \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --revision-suffix $(Get-Date -Format 'yyyyMMddHHmmss')
```

Services:
- `ca-events-api-gateway-dev`
- `ca-events-prospect-service-dev`
- `ca-events-student-service-dev`
- `ca-events-instructor-service-dev`
- `ca-events-event-relay-dev`
- `ca-events-projection-service-dev`
- `ca-events-frontend-dev`

### 4. Verify Deployment

```powershell
# List all container apps with status
az containerapp list \
  --resource-group rg-events-dev \
  --query "[].{Name:name, Status:properties.runningStatus, FQDN:properties.configuration.ingress.fqdn}" \
  -o table

# Test API Gateway
curl https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/health

# Test Frontend
curl https://ca-events-frontend-dev.icyhill-68ffa719.westus2.azurecontainerapps.io
```

## Troubleshooting

### Image Not Updating After Build

**Symptom:** Container app still running old code after ACR build completes.

**Solution:**
```powershell
# Force revision update
az containerapp update \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --revision-suffix $(Get-Date -Format 'yyyyMMddHHmmss')
```

### Terraform Version Mismatch

**Symptom:** Error about unsupported Terraform Core version.

**Solution:**
```powershell
cd infrastructure
Remove-Item -Recurse -Force .terraform
terraform init -upgrade
```

Requires Terraform >= 1.11 due to AVM module dependencies.

### Container App Health Check Failing

**Symptom:** Container app shows unhealthy status.

**Solution:**
```bash
# View logs
az containerapp logs show \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --follow

# Check revision status
az containerapp revision list \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  -o table
```

Common causes:
- Missing environment variables
- Key Vault access denied (check managed identity permissions)
- SQL connection string incorrect
- Service Bus connection issues

### GitHub Actions Authentication Errors

**Symptom:** "Failed to fetch federated token" or "Unable to get ACTIONS_ID_TOKEN_REQUEST_URL"

**Solution:**
The workflow uses `azure/login@v1` with service principal authentication (not OIDC).

Verify GitHub secrets are set correctly:
```powershell
gh secret list
```

Should see:
- ARM_CLIENT_ID
- ARM_CLIENT_SECRET
- ARM_SUBSCRIPTION_ID
- ARM_TENANT_ID

Re-run setup if needed:
```powershell
.\setup-github-secrets.ps1
```

### WebSocket Connection Refused

**Symptom:** Frontend cannot establish WebSocket connection.

**Solution:**
1. Verify API Gateway is running
2. Check CORS configuration in API Gateway
3. For development: Ensure unauthenticated access is enabled
4. Check network security groups/firewall rules
5. Verify WebSocket endpoint URL in frontend config

## Deployment Architecture

### Build Process

All images are built directly in Azure Container Registry using `az acr build`:
- Bypasses local Docker daemon issues
- Faster deployment to Azure (no local image push required)
- Consistent build environment
- Automatic image tagging with `latest`
- Supports building from GitHub repository URL

### Infrastructure Updates

Terraform manages all Azure resources using Azure Verified Modules (AVM):
- **Container Apps** and Environment (auto-scaling, ingress configuration)
- **Event Grid** topics (`prospect-events`, `student-events`, `instructor-events`)
- **Service Bus** namespace with `identity-commands` queue and DLQ topics
- **SQL Databases** (transactional and read models, Basic tier)
- **Application Insights** for observability
- **Key Vault** for secrets management
- **Log Analytics** workspace for centralized logging
- **RBAC permissions** (ACR pull, Key Vault access, managed identities)

### Container App Updates

Container Apps use rolling update strategy:
1. Build new images with `latest` tag in ACR
2. Apply infrastructure changes (if any)
3. Restart container apps to pull new images
4. Health checks verify successful deployment
5. Old revisions automatically deactivated after new revision is healthy

## Monitoring

### View Build Logs

```powershell
# List recent ACR builds
az acr task list-runs --registry acreventsdevrcwv3i --top 10 -o table

# Get detailed logs for specific build
az acr task logs --registry acreventsdevrcwv3i --run-id <run-id>
```

### View Container Logs

```powershell
# Follow logs in real-time
az containerapp logs show \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --follow

# Get last 100 lines
az containerapp logs show \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --tail 100
```

### View GitHub Actions Workflow

```powershell
# List recent workflow runs
gh run list --workflow="deploy-azure.yml"

# View specific run logs
gh run view <run-id> --log

# Watch current run
gh run watch
```

### Application Insights

View real-time telemetry in Azure Portal:
1. Navigate to Application Insights resource: `appi-events-dev`
2. Check **Live Metrics** for real-time data
3. Review **Failures** for errors
4. Analyze **Performance** for slow requests
5. Query **Logs** using KQL

## Resource URLs

After deployment, access services at:

- **Frontend**: https://ca-events-frontend-dev.icyhill-68ffa719.westus2.azurecontainerapps.io
- **API Gateway**: https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io
- **WebSocket**: wss://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/ws/events
- **API Docs**: https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/swagger
- **Outbox API**: https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/api/outbox

## Cleanup

To remove all deployed resources:

```powershell
# Destroy infrastructure
cd infrastructure
terraform destroy

# Or delete resource group (faster)
az group delete --name rg-events-dev --yes --no-wait
```

**Warning:** This will delete all data and cannot be undone.
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
