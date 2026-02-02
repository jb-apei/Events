# Terraform Best Practices - Service URL Management

## Problem Statement

When deploying microservices in Azure Container Apps, services need to communicate with each other. This requires knowing each service's URL/FQDN. The challenge: **How do we reference service URLs in Terraform without creating circular dependencies?**

## The Challenge

```hcl
# ❌ PROBLEM: Circular Dependency
module "container_apps" {
  apps = {
    prospect-service = {
      env_vars = {
        # prospect-service needs API Gateway URL...
        ApiGateway__Url = module.container_apps.api_gateway_url
      }
    }
    api-gateway = {
      # ...but API Gateway is created in the same module!
      # Terraform Error: Cycle detected
    }
  }
}
```

## Solution Approaches Evaluated

### ❌ Approach 1: Hardcoded URLs
```hcl
ApiGateway__Url = "https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io"
```

**Pros**: Simple, works immediately  
**Cons**: 
- Brittle - breaks if naming changes
- Environment-specific - can't reuse across dev/test/prod
- Manual updates required
- No validation at plan time
- Difficult to maintain

**Verdict**: ❌ **Not recommended** - violates Infrastructure as Code principles

---

### ❌ Approach 2: Two-Stage Deployment
```hcl
# Stage 1: Create API Gateway
module "api_gateway" { ... }

# Stage 2: Create other services referencing API Gateway
module "other_services" {
  api_gateway_url = module.api_gateway.fqdn
}
```

**Pros**: No circular dependency, clean separation  
**Cons**:
- Complex module structure
- Two separate apply operations required
- Harder to manage in CI/CD
- State management complexity
- More expensive (two deployments)

**Verdict**: ❌ **Overkill** - adds unnecessary complexity

---

### ❌ Approach 3: Post-Deployment Updates
```hcl
# 1. Deploy all apps with placeholder URLs
# 2. Use data source to fetch actual URLs
# 3. Update apps with real URLs
```

**Pros**: Works around circular dependency  
**Cons**:
- Requires two apply cycles
- Services broken between deploys
- Inefficient
- Complex state management
- Poor developer experience

**Verdict**: ❌ **Fragile** - creates deployment downtime

---

### ✅ Approach 4: Locals with Computed Values (RECOMMENDED)
```hcl
# Container Apps Environment is created first and provides default_domain
module "container_apps_environment" {
  name = "cae-${var.project_name}-${var.environment}"
}

# Compute service URLs using the environment's domain
locals {
  container_app_base_domain = module.container_apps_environment.default_domain
  
  # Construct FQDNs using Azure's naming pattern
  api_gateway_fqdn = "ca-${var.project_name}-api-gateway-${var.environment}.${local.container_app_base_domain}"
  api_gateway_url  = "https://${local.api_gateway_fqdn}"
  
  prospect_service_fqdn = "ca-${var.project_name}-prospect-service-${var.environment}.${local.container_app_base_domain}"
}

# Use computed locals in app configurations
module "container_apps" {
  apps = {
    prospect-service = {
      env_vars = {
        ApiGateway__Url = local.api_gateway_url  # ✅ Clean reference
      }
    }
    api-gateway = {
      # Created simultaneously - no circular dependency
    }
  }
}
```

**Pros**:
- ✅ **No circular dependencies** - environment exists before apps
- ✅ **Type-safe** - Terraform validates at plan time
- ✅ **Maintainable** - centralized in locals block
- ✅ **DRY principle** - reuse naming patterns
- ✅ **Environment-agnostic** - works across dev/test/prod
- ✅ **Self-documenting** - clear URL construction logic
- ✅ **Validated** - Terraform plan confirms correctness

**Cons**:
- Assumes Azure's naming convention doesn't change (low risk)
- Requires understanding of Container Apps FQDN structure

**Verdict**: ✅ **BEST PRACTICE** - industry-standard approach

---

## Implementation Details

### Azure Container Apps FQDN Pattern

Azure Container Apps use this FQDN structure:
```
{container-app-name}.{environment-default-domain}
```

Example:
- Environment domain: `orangehill-95ada862.eastus2.azurecontainerapps.io`
- Container App name: `ca-events-api-gateway-dev`
- **Full FQDN**: `ca-events-api-gateway-dev.orangehill-95ada862.eastus2.azurecontainerapps.io`

### Locals Block Pattern

```hcl
locals {
  # 1. Get base domain from environment (created earlier in dependency graph)
  container_app_base_domain = module.container_apps_environment.default_domain
  
  # 2. Construct FQDNs for all services
  api_gateway_fqdn        = "ca-${var.project_name}-api-gateway-${var.environment}.${local.container_app_base_domain}"
  prospect_service_fqdn   = "ca-${var.project_name}-prospect-service-${var.environment}.${local.container_app_base_domain}"
  projection_service_fqdn = "ca-${var.project_name}-projection-service-${var.environment}.${local.container_app_base_domain}"
  
  # 3. Create full URLs
  api_gateway_url = "https://${local.api_gateway_fqdn}"
}
```

### Usage in Container Apps

```hcl
module "container_apps" {
  apps = {
    prospect-service = {
      env_vars = {
        ApiGateway__Url        = local.api_gateway_url        # ✅ From locals
        ApiGateway__PushEvents = "true"
      }
    }
    
    projection-service = {
      env_vars = {
        ApiGateway__Url = local.api_gateway_url               # ✅ Reused
      }
    }
    
    frontend = {
      env_vars = {
        API_GATEWAY_URL = local.api_gateway_url               # ✅ Consistent
      }
    }
  }
}
```

---

## Benefits

### 1. Dependency Graph Resolution
Terraform's dependency graph:
```
Environment → Locals → Container Apps
     ↓           ↓            ↓
 (outputs)  (computed)  (use locals)
```

No cycles because locals **depend on** environment but **not on** container apps.

### 2. Validation at Plan Time
```bash
$ terraform plan
...
+ env_vars = {
+   ApiGateway__Url = "https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io"
+ }
```

You see the exact URL **before** applying, ensuring correctness.

### 3. Centralized Management
```hcl
# Change naming pattern in ONE place
locals {
  api_gateway_fqdn = "ca-${var.project_name}-gateway-${var.environment}.${local.container_app_base_domain}"
  #                                        ^^^^^^^ Changed: "api-gateway" → "gateway"
}

# All references update automatically! No find-and-replace needed
```

### 4. Environment Isolation
```hcl
# dev environment
var.environment = "dev"
→ "https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io"

# prod environment
var.environment = "prod"
→ "https://ca-events-api-gateway-prod.purplefield-12345678.westus2.azurecontainerapps.io"
```

Same code, different environments - no hardcoding required.

---

## Testing & Validation

### Validate Syntax
```bash
terraform validate
# Success! The configuration is valid.
```

### Preview Changes
```bash
terraform plan
# Plan: 0 to add, 3 to change, 0 to destroy
# (Only Event Grid webhook updates, no container app changes)
```

### Verify Computed Values
```bash
terraform console
> local.api_gateway_url
"https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io"
```

---

## Migration Guide

If you have hardcoded URLs in existing Terraform:

### Step 1: Add Locals Block
```hcl
locals {
  container_app_base_domain = module.container_apps_environment.default_domain
  api_gateway_url           = "https://ca-${var.project_name}-api-gateway-${var.environment}.${local.container_app_base_domain}"
}
```

### Step 2: Replace Hardcoded URLs
```diff
- ApiGateway__Url = "https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io"
+ ApiGateway__Url = local.api_gateway_url
```

### Step 3: Validate & Apply
```bash
terraform validate
terraform plan  # Verify no infrastructure changes
terraform apply
```

---

## Common Pitfalls

### ❌ Wrong: Using Module Outputs
```hcl
# This creates circular dependency!
ApiGateway__Url = module.container_apps.container_app_fqdns["api-gateway"]
```

### ✅ Correct: Using Locals
```hcl
ApiGateway__Url = local.api_gateway_url
```

---

### ❌ Wrong: Hardcoding Domain
```hcl
locals {
  # Don't hardcode the environment domain!
  api_gateway_url = "https://ca-events-api-gateway-dev.icyhill-68ffa719.westus2.azurecontainerapps.io"
}
```

### ✅ Correct: Computing Domain
```hcl
locals {
  # Use module output for environment domain
  container_app_base_domain = module.container_apps_environment.default_domain
  api_gateway_url = "https://ca-${var.project_name}-api-gateway-${var.environment}.${local.container_app_base_domain}"
}
```

---

## Conclusion

The **locals with computed values** approach is the industry-standard solution for managing inter-service URLs in Terraform. It balances:

- ✅ **Simplicity** - easy to understand and maintain
- ✅ **Correctness** - validated at plan time
- ✅ **Flexibility** - works across all environments
- ✅ **Maintainability** - centralized configuration
- ✅ **Performance** - single apply operation

**Recommendation**: Always use this pattern when services need to reference each other's URLs.

---

## References

- [Terraform Locals](https://developer.hashicorp.com/terraform/language/values/locals)
- [Azure Container Apps FQDN Structure](https://learn.microsoft.com/en-us/azure/container-apps/networking)
- [Terraform Dependency Graph](https://developer.hashicorp.com/terraform/internals/graph)
