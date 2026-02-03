# Solution for Event Grid & Projection Service Circular Dependency

## The "Chicken and Egg" Problem
In Terraform, we have a circular dependency issue during the initial deployment (or when recreating infrastructure):

1.  **Event Grid Subscription** requires a **Valid Webhook URL** to be created.
    *   Azure attempts to send an `OPTIONS` request to the URL (`https://.../events/webhook`) to validate ownership.
    *   If the endpoint is not reachable or returns an error, Terraform fails the deployment.
2.  **Projection Service** (Container App) provides that Webhook URL.
    *   It cannot successfully deploy and become "Healthy" if the Terraform run fails (e.g. if it's part of the same `apply` that is failing on the subscription).
    *   Often, the Container App depends on configuration or other resources in the same module.

**Result**: Terraform tries to create the subscription, the app isn't ready to respond to the validation handshake, Terraform fails, and the app never finishes deploying.

## Workaround Strategy

To resolve this, we must ensure the `Projection Service` is **Running and Healthy** before Terraform attempts to create the `EventGrid Event Subscription`.

### Option 1: Two-Stage Deployment (Manual/Scripted) - *Current Approach*
1.  **Stage 1 (App Only)**: 
    *   Comment out the `azurerm_eventgrid_event_subscription` resource in Terraform.
    *   Run `terraform apply`.
    *   This deploys the `projection-service` container app.
2.  **Verify Health**: Ensure the app is running (`az containerapp show ...`).
3.  **Stage 2 (Subscriptions)**:
    *   Uncomment the subscription resource.
    *   Run `terraform apply` again.
    *   Since the app is running, the validation handshake succeeds.

### Option 2: Terraform `depends_on` (Best Effort)
Ensure the subscription explicitly depends on the container app *and* potentially a null_resource that waits for health.
*Note: This is unreliable because Terraform considers the Container App "created" as soon as the API returns, which might be before the app is actually serving traffic.*

### Option 3: Automated "Wait" Hook (Recommended Automation)
Use a `null_resource` with a `local-exec` provisioner to poll the health endpoint before creating the subscription.

```hcl
resource "null_resource" "wait_for_projection_service" {
  triggers = {
    app_id = module.container_apps.container_app_ids["projection-service"]
  }

  provisioner "local-exec" {
    command = <<EOT
      # Powershell or Bash script to poll endpoint until 200 OK
      $url = "https://${module.container_apps.container_app_fqdns["projection-service"]}/health"
      for ($i=0; $i -lt 30; $i++) {
        try {
          $res = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -ErrorAction SilentlyContinue
          if ($res.StatusCode -eq 200) { exit 0 }
        } catch {}
        Start-Sleep -s 10
      }
      exit 1
    EOT
    interpreter = ["PowerShell", "-Command"]
  }
}

resource "azurerm_eventgrid_event_subscription" "projection_subscriptions" {
  depends_on = [null_resource.wait_for_projection_service]
  # ... rest of config
}
```

### Option 4: Validation Bypass (Not recommended for Prod)
Azure Event Grid allows manual validation, but this is difficult to automate with Terraform.

## Current Status (Feb 2026)
We successfully used **Option 1** to unblock the deployment. The Service Bus and Projection Service are now running, and subscriptions have been re-enabled.
