# Implementation Plan: Permission-Based Authorization Migration

**Branch**: `002-iam-integration` | **Date**: 2025-12-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-iam-integration/spec.md`

## Summary
Migrate the OrderService from a role-based/policy-based authorization system to a fine-grained permission-based system. This will involve registering ~15 permissions and 5 predefined roles (`roles.order.*`) with a central IAM service using two distinct registration calls. We will leverage the shared `IAMRegistrationService` base class from `ServiceDefaults`, implement a caching layer for permissions to meet latency targets, and update all controllers to use a new `[RequirePermission]` attribute.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: Microsoft.AspNetCore.Authorization, StackExchange.Redis, OpenTelemetry, Maliev.Aspire.ServiceDefaults
**Storage**: Redis (Caching), PostgreSQL (Audit logs)
**Testing**: xUnit, WebApplicationFactory with Testcontainers
**Target Platform**: Linux (Docker/Kubernetes)
**Project Type**: Web API (ASP.NET Core)
**Performance Goals**: <50ms authorization check latency, <5s startup time
**Constraints**: Fail-secure (deny access if IAM is down), code-first definitions, Principle XII metrics compliant, Principle XIII Aspire integrated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Follows `order.{resource}.{action}` naming convention.
- [x] Predefined roles match business requirements.
- [x] Caching strategy meets performance goals.
- [x] Security: Fail-secure is implemented.

## Project Structure

### Documentation (this feature)

```text
specs/002-iam-integration/
├── spec.md              # Requirements
├── plan.md              # This file
├── research.md          # Technical design
├── data-model.md        # DTOs and Cache models
├── quickstart.md        # Usage guide
└── contracts/
    └── iam-service.md   # External API contracts
```

### Source Code (repository root)

```text
Maliev.OrderService.Api/
├── Authorization/
│   ├── OrderPermissions.cs          # Permission constants
│   ├── OrderPredefinedRoles.cs      # Role definitions
│   ├── RequirePermissionAttribute.cs # Custom attribute
│   └── PermissionAuthorizationHandler.cs # Logic
├── Services/
│   ├── External/
│   │   ├── IIamServiceClient.cs      # IAM communication
│   │   └── IamServiceClient.cs
│   └── OrderIAMRegistrationService.cs # Startup sync
├── Controllers/
│   └── ... (Updated with [RequirePermission])

Maliev.OrderService.Tests/
├── Testing/
│   └── BaseIntegrationTestFactory.cs (Updated with permission helpers)
└── Contract/
    └── ... (Updated tests)
```

**Structure Decision**: Standard ASP.NET Core Web API structure with a dedicated `Authorization` folder for the new infrastructure.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Custom Authorization Handler | Fine-grained permission logic | Standard policies are too static for dynamic IAM roles |
| Background Service for Registration | Automatic sync on deployment | Manual sync is error-prone and lags behind code changes |
| Redis Caching for Permissions | Performance (<50ms) | Direct IAM calls on every request would be too slow |
