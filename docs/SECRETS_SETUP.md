# GitHub Secrets Setup for Azure Deployment

The GitHub Actions workflow requires the following secrets to be configured in your repository.

## Required Secrets

Navigate to: **Settings → Secrets and variables → Actions → New repository secret**

### 1. ARM_CLIENT_ID
The Azure Service Principal Application (client) ID.

```bash
# Get from your service principal
az ad sp list --display-name <your-service-principal-name> --query "[0].appId" -o tsv
```

### 2. ARM_CLIENT_SECRET
The Azure Service Principal password/secret.

```bash
# This was provided when you created the service principal
# If you need to reset it:
az ad sp credential reset --id <client-id> --query password -o tsv
```

### 3. ARM_SUBSCRIPTION_ID
Your Azure Subscription ID.

```bash
# Get your subscription ID
az account show --query id -o tsv
```

### 4. ARM_TENANT_ID
Your Azure Active Directory Tenant ID.

```bash
# Get your tenant ID
az account show --query tenantId -o tsv
```

## Creating a Service Principal (if needed)

If you don't have a service principal yet, create one:

```bash
# Set variables
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP="rg-events-dev"

# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-events" \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth false

# Output will show:
# {
#   "appId": "<ARM_CLIENT_ID>",
#   "password": "<ARM_CLIENT_SECRET>",
#   "tenant": "<ARM_TENANT_ID>"
# }
```

## Additional Permissions

The service principal needs additional permissions for ACR:

```bash
# Get service principal object ID
SP_OBJECT_ID=$(az ad sp list --display-name "github-actions-events" --query "[0].id" -o tsv)

# Grant ACR push permission
az role assignment create \
  --assignee $SP_OBJECT_ID \
  --role "AcrPush" \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.ContainerRegistry/registries/acreventsdevrcwv3i
```

## Verify Setup

After adding all secrets, the GitHub Actions workflow will:
1. ✅ Build all container images and push to ACR
2. ✅ Apply Terraform infrastructure changes
3. ✅ Restart container apps with new images

## Workflow Authentication Method

The workflow uses **OpenID Connect (OIDC)** authentication with individual client ID, tenant ID, and subscription ID instead of the deprecated `AZURE_CREDENTIALS` JSON format.

**Old format (deprecated):**
```yaml
- uses: azure/login@v1
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}  # JSON blob - NO LONGER USED
```

**New format (current):**
```yaml
- uses: azure/login@v2
  with:
    client-id: ${{ secrets.ARM_CLIENT_ID }}
    tenant-id: ${{ secrets.ARM_TENANT_ID }}
    subscription-id: ${{ secrets.ARM_SUBSCRIPTION_ID }}
```

This matches the Terraform authentication variables already in use.
