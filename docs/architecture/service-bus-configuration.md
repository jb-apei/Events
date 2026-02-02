# Service Bus & Messaging Configuration

## Overview
This document details the configuration of Azure Service Bus and Azure Event Grid used in the Events project.

---

## 1. Azure Service Bus

Used primarily for the **Command Path** (sending commands to the backend services securely and reliably).

### Configuration
| Setting | Value | Description |
| :--- | :--- | :--- |
| **Namespace Pattern** | `sb-events-{env}-{random}` | e.g., `sb-events-dev-ab12cd` |
| **Pricing Tier** | **Standard** | Supports Topics/Queues, but no predictable latency (Premium required for VNet). |
| **Authentication** | **Connection Strings** | Currently, services use `Shared Access Signatures` (Connection String) injected via Key Vault. <br> *Note: Migration to RBAC (Managed Identity) is planned.* |

### Queues

#### `identity-commands`
The primary ingress queue for all write commands (CQRS Command path).

| Property | Value | Rationale |
| :--- | :--- | :--- |
| **Max Delivery Count** | 10 | Retries before moving to DLQ. |
| **Time to Live (TTL)** | 14 Days | Ensure commands aren't lost if services are down for extended periods. |
| **Duplicate Detection** | Enabled | Prevents processing the same command twice. |
| **Dup Detect Window** | 10 Minutes | Window for checking `MessageId`. |
| **Dead Lettering** | On Expiration | Expired messages go to DLQ for audit. |

### Topics
*Currently, Service Bus Topics are not the primary event distribution mechanism (see Event Grid below), but the following are provisioned:*

- **`prospect-dlq`**: Reserved for future dead-letter processing or specific audit streams.

---

## 2. Azure Event Grid

Used for the **Event Path** (Pub/Sub fan-out to read models and UI).

### Configuration
- **Type**: Event Grid Topics (Custom)
- **Schema**: CloudEvent Schema v1.0
- **Authentication**: Key-based (Access Keys injected into `EventRelay` service).

### Topics
| Topic Name | Publisher | Purpose |
| :--- | :--- | :--- |
| `prospect-events` | `EventRelay` | Public events regarding Prospects (Created, Updated). |
| `student-events` | `EventRelay` | Public events regarding Students. |
| `instructor-events` | `EventRelay` | Public events regarding Instructors. |

### Subscriptions
Currently, the **Projection Service** subscribes to all topics via Webhook.

- **Name**: `sub-{topic}-to-projection`
- **Endpoint**: `https://{projection-service-url}/events/webhook`
- **Retry Policy**:
    - Max Attempts: 30
    - TTL: 24 Hours

---

## 3. Provisioning Roles & requirements

To provision the infrastructure (Terraform), the following permissions are required on the subscription/resource group:

1.  **Contributor**: To create Resources (Service Bus, Event Grid, Container Apps).
2.  **User Access Administrator**: To assign roles (e.g., granting `AcrPull` to Container Apps).
3.  **Key Vault Administrator** (or specific policy): To seed secrets into Key Vault.

### Runtime Roles (Future State)
*Current implementation uses Connection Strings. When migrating to RBAC, the following roles will be needed:*

- **Azure Service Bus Data Sender**: For API Gateway (sending commands).
- **Azure Service Bus Data Receiver**: For Command Handlers (processing commands).
- **EventGrid Data Sender**: For Event Relay.
