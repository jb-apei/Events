# ADR-006: Azure Container Apps (ACA) for Compute

**Status:** Accepted  
**Date:** 2026-01-30  
**Deciders:** Architecture Team

## Context

The Events project consists of multiple independent microservices (Gateway, Prospect, Student, etc.) that need to scale independently. We require a hosting platform that supports HTTP ingress, background processing (TCP/Queue), and scale-to-zero capabilities to minimize costs during low traffic.

## Decision

We will use **Azure Container Apps (ACA)** as our primary compute platform.

## Rationale

### Pros
- **Serverless Containers**: Abstraction over Kubernetes (KEDA, Envoy, Dapr) without the operational overhead of managing an AKS cluster.
- **Scale-to-Zero**: Services like `StudentService` or `InstructorService` can scale down to 0 replicas when idle, significantly reducing costs for MVP/Dev environments.
- **KEDA Integration**: Native support for scaling based on Service Bus Queue depth (critical for our async command handling).
- **Revision Management**: Built-in support for zero-downtime deployments via Revisions, allowing Blue/Green deployment patterns out of the box.

### Cons
- **Cold Starts**: Scaling from zero introduces initial latency (mitigated by `minReplicas=1` for critical paths like API Gateway).
- **Cost at Scale**: For very high, constant load, AKS reserved instances may be cheaper (though ACA is cheaper for variable/bursty workloads).

## Alternatives Considered

### 1. Azure Kubernetes Service (AKS)
- **Rejected**: Operational complexity (upgrades, node pools, security patching) is too high for the current team size and project phase.

### 2. Azure App Service (Web Apps for Containers)
- **Rejected**: Poor support for event-driven scaling (KEDA) and microservices orchestration (service discovery). Building 7 App Service Plans would be cost-prohibitive.

## Implementation Details

- **Environment**: Global `Container Apps Environment` acts as the secure boundary.
- **Ingress**: `External` for Gateway/Frontend, `Internal` (or none) for backend services.
- **Registry**: Azure Container Registry (ACR) with Managed Identity pull access.
