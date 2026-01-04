# Maliev Order Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.OrderService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Mission-critical order orchestration engine for the Maliev manufacturing ecosystem.

**Role in MALIEV Architecture**: The central orchestrator for all commercial transactions. It manages the complete 16-state lifecycle of a manufacturing order, coordinating between Customers, Materials, Payments, and Fulfillment services to ensure precise execution and delivery.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (High-speed material resolution)
- **Messaging**: RabbitMQ via MassTransit
- **State Machine**: Industrial-grade 16-state workflow engine
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **16-State Workflow Engine**: Sophisticated order lifecycle management with built-in validation for complex manufacturing transitions.
- **Smart Batch Operations**: High-performance, transactional batch create/update/cancel for enterprise-scale ordering.
- **Optimistic Concurrency**: Precise conflict detection using RowVersion ensures data integrity during high-concurrency operations.
- **Sequential ID Generation**: Guaranteed atomic order numbering (ORD-YYYY-XXXXX) with annual resets for clear tracking.
- **Rich Technical Attributes**: Specialized metadata handling for diverse service categories (3D Printing, CNC, Design, Scanning).

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.OrderService.git
cd Maliev.OrderService
```

2. **Spin up Infrastructure**
```bash
docker run --name order-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name order-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__OrderDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.OrderService.Data
dotnet run --project Maliev.OrderService.Api
```

The service will be available at `http://localhost:5000/order`. Access the interactive documentation at `http://localhost:5000/order/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/order/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/orders` | List and filter orders (paginated) |
| POST | `/orders` | Create a new manufacturing order |
| POST | `/orders/batch` | Transactional batch order creation |
| POST | `/orders/{id}/statuses` | Trigger a workflow state transition |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /order/liveness`
- **Readiness**: `GET /order/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /order/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-order-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
