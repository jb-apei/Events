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

The CI/CD pipeline has been optimized into two specialized workflows to reduce build times and ensure efficiency.

**1. Deploy Infrastructure (`deploy-infrastructure.yml`)**
- **Triggers**: Changes to `infrastructure/**`
- **Action**: Runs `terraform plan` and `terraform apply`.
- **Purpose**: Keeps Azure resources (databases, service bus, etc.) in sync.
- **Speed**: Ignored when only application code changes.

**2. Deploy Services (`deploy-services.yml`)**
- **Triggers**: Changes to `src/**`
- **Action**: Dynamically detects changed services, tests them, builds docker images, and deploys updates.
- **Features**:
    - **Path Filtering**: Only builds what changed.
        - *Frontend change* → Builds only Frontend (~2 mins).
        - *ProspectService change* → Builds only ProspectService (~3 mins).
        - *Shared Code change* → Builds ALL services (~10 mins).
    - **Dynamic Discovery**: Automatically finds any folder in `src/services/` that contains a `Dockerfile`.
        - Naming Convention: Folder `BillingService` → Image `billing-service` → App `ca-events-billing-service-dev`.
        - No YAML configuration required for new services.
    - **Parallel Deployment**: Uses a matrix strategy to deploy all updated services simultaneously.

**handling Deletions:**
- If a service folder is deleted, the pipeline automatically ignores it (no build/deploy).
- **Important**: You must manually remove the service definition from Terraform (`infrastructure/main.tf`) to delete the Azure resource and stop billing.

**Trigger Workflows Manually:**
```powershell
# Infrastructure ONLY
gh workflow run deploy-infrastructure.yml --ref master

# Services ONLY
gh workflow run deploy-services.yml --ref master
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

---

## Troubleshooting

### Common Deployment Issues

#### 1. Terraform Version Error

**Problem:**
```
Error: Unsupported Terraform Core version
Module does not support Terraform version 1.6.0
```

**Solution:**
The project requires Terraform >= 1.9. Update GitHub Actions workflow:

```yaml
env:
  TERRAFORM_VERSION: 1.14.4  # Must be >= 1.9
```

Locally:
```powershell
terraform version  # Check current version
# Install Terraform 1.14.4 or later
```

#### 2. Container App Won't Start

**Problem:** Container app stuck in "Provisioning" or crashes immediately

**Diagnosis:**
```powershell
# Check logs
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

**Common Causes:**
- **Missing environment variables**: Check Container Apps environment variables match Terraform config
- **Key Vault access denied**: Ensure managed identity has Get/List secrets permissions
- **Image pull failure**: Verify ACR pull role assignment exists
- **Port mismatch**: Container must expose port defined in Terraform (8080 for most services)

**Solutions:**
```powershell
# Grant Key Vault access to container app
$principalId = (az containerapp show --name ca-events-api-gateway-dev --resource-group rg-events-dev --query identity.principalId -o tsv)

az keyvault set-policy \
  --name kv-events-dev-rcwv3i \
  --object-id $principalId \
  --secret-permissions get list

# Verify ACR pull permissions
az role assignment list \
  --assignee $principalId \
  --scope /subscriptions/<sub-id>/resourceGroups/rg-events-dev/providers/Microsoft.ContainerRegistry/registries/acreventsdevrcwv3i
```

#### 3. WebSocket Connection Refused

**Problem:** Frontend cannot connect to WebSocket endpoint

**Diagnosis:**
```powershell
# Test WebSocket from PowerShell (requires websocat tool)
websocat wss://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/ws/events

# Or use JavaScript in browser console
const ws = new WebSocket('wss://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/ws/events');
ws.onopen = () => console.log('Connected');
ws.onerror = (err) => console.error('Error:', err);
```

**Common Causes:**
- **HTTPS/WSS mismatch**: Use `wss://` in production, `ws://` only locally
- **CORS issues**: Check API Gateway CORS configuration allows frontend origin
- **Ingress not external**: Container Apps ingress must have `external_ingress = true`

**Solutions:**
```terraform
# Verify in Terraform (infrastructure/main.tf)
api-gateway = {
  ingress_enabled = true
  external_ingress = true  # Must be true for WebSocket
}
```

#### 4. Events Not Publishing

**Problem:** Events saved to Outbox but not appearing in Event Grid

**Diagnosis:**
```powershell
# Check Outbox for unpublished events
# (Requires connecting to SQL database)
sqlcmd -S sql-events-dev.database.windows.net -d db-events-transactional-dev -U sqladmin -P <password> -Q "SELECT TOP 10 * FROM Outbox WHERE Published = 0 ORDER BY CreatedAt DESC"

# Check EventRelay service logs
az containerapp logs show \
  --name ca-events-event-relay-dev \
  --resource-group rg-events-dev \
  --follow
```

**Common Causes:**
- **EventRelay service not running**: Check container status
- **Event Grid topic misconfigured**: Verify topic endpoints in Terraform
- **Network connectivity**: EventRelay can't reach Event Grid (firewall/NSG issue)
- **Throttling**: Event Grid rate limits exceeded

**Solutions:**
```powershell
# Restart EventRelay service
az containerapp update \
  --name ca-events-event-relay-dev \
  --resource-group rg-events-dev

# Verify Event Grid topic exists
az eventgrid topic list --resource-group rg-events-dev -o table

# Test Event Grid endpoint
curl -X POST https://evgt-events-prospect-events-dev.westus2-1.eventgrid.azure.net/api/events \
  -H "aeg-sas-key: <topic-key>" \
  -H "Content-Type: application/cloudevents+json" \
  -d '[{"specversion":"1.0","type":"Test","source":"/test","id":"test","time":"2026-01-30T00:00:00Z"}]'
```

#### 5. Terraform State Lock

**Problem:**
```
Error: Error acquiring the state lock
Lock Info: ID <lock-id>
```

**Solution:**
```powershell
# If no other terraform apply is running, force unlock
cd infrastructure
terraform force-unlock <lock-id>

# Or wait for lock to expire (usually 15 minutes)
```

#### 6. SQL Connection Failures

**Problem:** Services can't connect to Azure SQL

**Diagnosis:**
```powershell
# Check SQL firewall rules
az sql server firewall-rule list \
  --resource-group rg-events-dev \
  --server sql-events-dev \
  -o table

# Test connection from Azure
az sql server show-connection-string \
  --client ado.net \
  --server sql-events-dev
```

**Common Causes:**
- **Firewall blocking**: Ensure "Allow Azure services" rule exists
- **Invalid credentials**: Check username/password in environment variables
- **Wrong connection string**: Verify format matches Azure SQL requirements
- **Database doesn't exist**: Run EF Core migrations

**Solutions:**
```powershell
# Add firewall rule for Azure services
az sql server firewall-rule create \
  --resource-group rg-events-dev \
  --server sql-events-dev \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Test connection from container app (via exec)
az containerapp exec \
  --name ca-events-api-gateway-dev \
  --resource-group rg-events-dev \
  --command /bin/bash
```

#### 7. GitHub Actions Authentication Failed

**Problem:**
```
Error: AADSTS700016: Application with identifier '<client-id>' was not found
```

**Solution:**
Service principal credentials are incorrect or expired. Regenerate:

```powershell
# Run setup script to recreate service principal and update GitHub secrets
.\setup-github-secrets.ps1

# Or manually create and add to GitHub
az ad sp create-for-rbac --name "github-actions-events" \
  --role contributor \
  --scopes /subscriptions/<subscription-id> \
  --sdk-auth
```

Then add output to GitHub secrets: ARM_CLIENT_ID, ARM_CLIENT_SECRET, ARM_SUBSCRIPTION_ID, ARM_TENANT_ID

#### 8. Image Build Failures in ACR

**Problem:** `az acr build` fails with "repository not found" or permission errors

**Diagnosis:**
```powershell
# Check ACR health
az acr check-health --name acreventsdevrcwv3i

# List repositories
az acr repository list --name acreventsdevrcwv3i -o table

# Check build tasks
az acr task-run list --registry acreventsdevrcwv3i -o table
```

**Solutions:**
```powershell
# Enable admin account (temporary for debugging)
az acr update --name acreventsdevrcwv3i --admin-enabled true

# Get credentials
az acr credential show --name acreventsdevrcwv3i

# Manual build test
az acr build --registry acreventsdevrcwv3i \
  --image api-gateway:test \
  --file src/services/ApiGateway/Dockerfile \
  .
```

#### 9. Event Grid Webhook Delivery Failures

**Problem:** Event Grid can't deliver events to ProjectionService

**Diagnosis:**
```powershell
# Check Event Grid subscription status
az eventgrid event-subscription list \
  --source-resource-id /subscriptions/<sub-id>/resourceGroups/rg-events-dev/providers/Microsoft.EventGrid/topics/evgt-events-prospect-events-dev \
  -o table

# View delivery failures
az eventgrid event-subscription show \
  --name sub-prospect-events-to-projection \
  --source-resource-id /subscriptions/<sub-id>/resourceGroups/rg-events-dev/providers/Microsoft.EventGrid/topics/evgt-events-prospect-events-dev \
  --query provisioningState
```

**Common Causes:**
- **Webhook endpoint not accessible**: ProjectionService ingress must allow Event Grid IPs
- **Invalid HTTPS certificate**: Container Apps should auto-provision valid certs
- **Endpoint returns non-2xx**: ProjectionService webhook handler has bugs

**Solutions:**
```powershell
# Test webhook endpoint manually
curl -X POST https://ca-events-projection-service-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/events/webhook \
  -H "Content-Type: application/json" \
  -d '{"test": "event"}'

# Check ProjectionService logs for errors
az containerapp logs show \
  --name ca-events-projection-service-dev \
  --resource-group rg-events-dev \
  --follow
```

#### 10. Frontend 404 or Blank Page

**Problem:** Accessing frontend URL returns 404 or blank page

**Diagnosis:**
```powershell
# Check if frontend container is running
az containerapp show \
  --name ca-events-frontend-dev \
  --resource-group rg-events-dev \
  --query properties.runningStatus

# Check ingress configuration
az containerapp show \
  --name ca-events-frontend-dev \
  --resource-group rg-events-dev \
  --query properties.configuration.ingress
```

**Common Causes:**
- **Build failed**: Vite build errors during Docker build
- **Nginx misconfigured**: nginx.conf has incorrect root or try_files
- **Port mismatch**: Dockerfile EXPOSE port doesn't match Terraform
- **Missing environment variables**: API_GATEWAY_URL not set

**Solutions:**
```powershell
# Check frontend logs
az containerapp logs show \
  --name ca-events-frontend-dev \
  --resource-group rg-events-dev

# Rebuild image
cd src/frontend
docker build -t acreventsdevrcwv3i.azurecr.io/frontend:latest .

# Verify nginx config locally
docker run -p 8080:80 acreventsdevrcwv3i.azurecr.io/frontend:latest
curl http://localhost:8080
```

---

## Monitoring & Observability

### Application Insights Queries

**Common KQL Queries:**

```kql
// Recent exceptions
exceptions
| where timestamp > ago(1h)
| order by timestamp desc
| project timestamp, type, outerMessage, innermostMessage

// Slow requests (>1 second)
requests
| where timestamp > ago(1h) and duration > 1000
| order by duration desc
| project timestamp, name, duration, resultCode

// Failed dependencies (Service Bus, SQL, Event Grid)
dependencies
| where timestamp > ago(1h) and success == false
| order by timestamp desc
| project timestamp, name, type, data, resultCode

// Event Grid webhook deliveries
requests
| where name contains "POST /events/webhook"
| where timestamp > ago(1h)
| project timestamp, resultCode, duration, customDimensions

// Outbox processing lag
traces
| where message contains "Outbox" and message contains "Published"
| where timestamp > ago(1h)
| order by timestamp desc
```

### Health Check Endpoints

All services expose `/health` endpoints:

```powershell
# API Gateway
curl https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/health

# ProjectionService
curl https://ca-events-projection-service-dev.icyhill-68ffa719.westus2.azurecontainerapps.io/health
```

### Alert Configuration

Recommended alerts in Azure Monitor:

1. **Container App Down**: `properties.runningStatus != 'Running'`
2. **High Error Rate**: `exceptions | where timestamp > ago(5m) | count > 10`
3. **Outbox Lag**: Custom metric if unpublished events > 100
4. **Event Grid DLQ**: Dead-letter queue depth > 10
5. **SQL DTU Usage**: > 80% for 5 minutes
6. **WebSocket Errors**: Failed connections > 5/minute

---

## Best Practices

### Deployment Strategy

1. **Always use GitHub Actions** for production deployments (auditability)
2. **Test locally first** with `deploy.ps1` before pushing to GitHub
3. **Review Terraform plan** before applying changes
4. **Deploy during low-traffic** windows when possible
5. **Monitor Application Insights** for 15 minutes post-deployment

### Rollback Strategy

1. Keep last 3 container revisions active
2. Use revision traffic splitting for canary deployments
3. Document rollback commands in runbook
4. Test rollback procedure monthly

### Security

1. **Never commit secrets** to git (use .gitignore)
2. **Rotate service principal credentials** quarterly
3. **Use managed identities** wherever possible (no connection strings)
4. **Enable Key Vault soft delete** and purge protection
5. **Audit access logs** in Key Vault monthly

### Cost Optimization

1. **Use consumption plan** for Container Apps (auto-scale to zero)
2. **Basic tier SQL** is sufficient for dev (upgrade for prod)
3. **Delete old container revisions** (keep only last 3)
4. **Monitor Event Grid costs** (charged per operation)
5. **Use Azure Cost Management** alerts

---

## Additional Resources

- [Architecture Documentation](Architecture.md) - System design details
- [Developer Guide](DEVELOPER_GUIDE.md) - Local development setup
- [Terraform Best Practices](TERRAFORM_BEST_PRACTICES.md) - Infrastructure patterns
- [Action Plan](ACTION_PLAN.md) - Planned improvements
- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure Event Grid Documentation](https://learn.microsoft.com/en-us/azure/event-grid/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
