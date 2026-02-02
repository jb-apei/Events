# Service Bus Patterns

## Overview
This document describes the messaging patterns used within the Events architecture to ensure reliability, scalability, and handling of large payloads.

---

## 1. Claim Check Pattern (Future Implementation)

### Problem
Azure Service Bus has a maximum message size limit (e.g., 256 KB or 1 MB depending on the tier). If an event payload contains large data (e.g., a student uploading a large document or a detailed transcript), it may exceed this limit.

### Pattern Description
The **Claim Check** pattern splits a large message into a "claim check" and a payload.
1.  **Store Payload**: The sender stores the full payload in an external store (e.g., Azure Blob Storage).
2.  **Send Claim Check**: The sender posts a message to the Service Bus containing a reference (URI or token) to the stored payload.
3.  **Retrieve Payload**: The receiver reads the message, extracts the reference, and retrieves the full payload from the external store.

### Planned Implementation

#### 1. Payload Storage
- **Store**: Azure Blob Storage
- **Container**: `event-payloads`
- **Retention**: Lifecycle policy to delete blobs after X days (matching event retention).

#### 2. Message Envelope Modification
The standard event envelope will optionally include a `payloadReference` field.

```json
{
  "eventId": "guid",
  "eventType": "StudentTranscriptUploaded",
  "data": null, // Empty for claim check
  "model": "claim-check", // Indicator
  "claimCheck": {
    "blobUri": "https://storage.blob.core.windows.net/events/...",
    "token": "sas-token-if-needed"
  }
}
```

#### 3. Client SDK
A shared library wrapper (`ServiceBusClientWrapper`) handles the logic transparently:
- **On Publish**: Check size. If > Threshold, upload to Blob, replace data with Claim Check.
- **On Consume**: Check if Claim Check exists. If yes, download Blob, deserialize to original type.

---

## 2. Dead Letter Queue (DLQ) Pattern

### Current Usage
Used for message handling failures to prevent data loss.

- **Process**: If a message cannot be processed after `MaxDeliveryCount` (default: 10) attempts, it is moved to the DLQ.
- **Monitoring**: Operations team monitors DLQ depth.
- **Recovery**: Manual inspection or repair-and-replay tools.

---

## 3. Transactional Outbox Pattern

### Current Usage
Ensures dual-write consistency between the Database and the Service Bus.

1.  **Commit**: Application writes entity state + Event to `Outbox` table in the same SQL transaction.
2.  **Relay**: A background worker (`EventRelay` service) polls the `Outbox` table.
3.  **Publish**: Worker publishes events to Service Bus/Event Grid.
4.  **Ack**: Upon successful publish, the Outbox record is marked as `Published`.

---

## 4. Pub/Sub Pattern (Topics & Subscriptions)

### Current Usage
We use Azure Service Bus Topics for events ensuring decoupling.

- **Publisher**: Doesn't know who listens. Publishes to `prospect-events` topic.
- **Subscribers**: Multiple services create subscriptions with filters.
    - `ProjectionService`: Subscribes to update Read Models.
    - `NotificationService`: Subscribes to send emails.
    - `AuditService`: Subscribes to log all activity.
