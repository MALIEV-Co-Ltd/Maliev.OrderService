# Maliev Order Service

Comprehensive order management microservice for Maliev Co. Ltd., a 3D printing and manufacturing business.

**Role in MALIEV Architecture**: Manages the complete order lifecycle from creation to fulfillment, including status tracking, file attachments, notes, batch operations, and cancellation with reason tracking. Integrates with Customer, Material, Payment, Upload, Auth, Employee, and Notification services.

---

## Architecture

- **Framework**: ASP.NET Core 10.0
- **Pattern**: 3-Layer (Api, Data, Tests)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Cache**: Redis 7.x distributed caching (24-hour material TTL)
- **Messaging**: RabbitMQ via MassTransit (for integration events)
- **Authentication**: JWT Bearer tokens (validated by Auth Service)
- **Authorization**: Role-based and resource-based policies with IAM integration
- **API Documentation**: OpenAPI with Scalar UI (development only) and comprehensive XML comments
- **Logging**: Serilog with structured logging and correlation IDs
- **Rate Limiting**: 100 req/min general, 10 req/min batch operations

---

## Constitution Rules

**Banned Libraries** (NOT used in this service):
- ❌ AutoMapper - Uses explicit manual mapping
- ❌ FluentValidation - Uses Data Annotations for validation
- ❌ FluentAssertions - Uses xUnit `Assert.*` methods
- ❌ In-memory test DB - Uses Testcontainers with real PostgreSQL

**Mandatory Practices**:
- ✅ **TreatWarningsAsErrors** enabled in all `.csproj` files
- ✅ **XML Documentation** on ALL public methods, properties, and classes
- ✅ **No Secrets in Code** - All secrets via environment variables
- ✅ **No Test Config in Program.js** - Test configuration in test fixtures only
- ✅ **IAM Integration** - Uses GCP-style permission naming: `order.orders.{action}`

---

## Features

- **16-State Order Workflow**: Complete order lifecycle with state machine validation
- **Batch Operations**: Transactional batch create/update/cancel (all-or-nothing)
- **Optimistic Concurrency**: RowVersion for conflict detection
- **Material Caching**: 24-hour TTL to reduce Material Service calls
- **File Management**: Upload Service integration with soft delete (30-day retention)
- **Dual Notes**: Customer-visible and internal-only notes
- **Status History**: Full audit trail of status changes
- **External Service Integration**: 7 services with retry policies
- **Sequential Order IDs**: ORD-{YYYY}-{NNNNN} with yearly reset
- **Rate Limiting**: Per-IP rate limiting with sliding windows

---

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 18
- Docker (optional, for Redis and RabbitMQ)
- Git

### Local Development Setup

1. **Clone repository**

    ```bash
    git clone https://github.com/MALIEV-Co-Ltd/Maliev.OrderService.git
    cd Maliev.OrderService
    ```

2. **Run database migrations**

    ```bash
    dotnet ef database update --project Maliev.OrderService.Data
    ```

3. **Run service**

    ```bash
    dotnet run --project Maliev.OrderService.Api
    ```

4. **Access Scalar UI (OpenAPI)**

    ```
    http://localhost:5000/order/scalar
    ```

---

## API Endpoints

**Base URL**: `{service-prefix}` (service prefix: `order`)

### Orders (9 endpoints)
```
GET    /v1/orders                      # List orders (paginated, filterable)
GET    /v1/orders/{orderId}            # Get order by ID
POST   /v1/orders                      # Create order
PUT    /v1/orders/{orderId}            # Update order (optimistic concurrency)
DELETE /v1/orders/{orderId}            # Cancel order
POST   /v1/orders/{orderId}/cancel     # Cancel with reason
GET    /v1/orders/{orderId}/statuses   # Get status history
GET    /v1/orders/{orderId}/files      # Get file list
GET    /v1/orders/{orderId}/notes      # Get notes list
```

### Batch Operations (3 endpoints)
```
POST   /v1/orders/batch                # Create multiple orders
PUT    /v1/orders/batch                # Update multiple orders
POST   /v1/orders/batch/cancel         # Cancel multiple orders
```

### Order Status (1 endpoint)
```
POST   /v1/orders/{orderId}/statuses   # Create status (state transition)
```

### Order Files (3 endpoints)
```
POST   /v1/orders/{orderId}/files      # Upload file (multipart, max 100MB)
GET    /v1/orders/{orderId}/files/{fileId}  # Download file
DELETE /v1/orders/{orderId}/files/{fileId}  # Delete file (soft delete)
```

### Order Notes (1 endpoint)
```
POST   /v1/orders/{orderId}/notes      # Create note (customer/internal)
```

**Total: 16 fully functional endpoints**

---

## Health Endpoints

**Standard health endpoints (all services):**
- `GET /order/liveness` - Kubernetes liveness probe
- `GET /order/readiness` - Kubernetes readiness probe (checks DB, Redis, RabbitMQ)
- `GET /order/metrics` - Prometheus metrics (OpenTelemetry)

---

## 16-State Order Workflow

```
New → Reviewing → [Rejected|Reviewed] → Quoted → [Declined|Accepted|Expired]
  → [Paid|POIssued] → InProgress → Finished → Shipped

Exception Flows:
  InProgress ↔ OnHold
  Finished/Shipped → Reopen → InProgress
  Any → Cancelled
```

---

## IAM Permissions

**GCP-style permission naming:**
- `order.orders.read` - View orders
- `order.orders.create` - Create orders
- `order.orders.update` - Update orders
- `order.orders.delete` - Cancel/delete orders
- `order.orders.batch` - Batch operations

---

## Database Schema

**DbContext**: `OrderDbContext`

**Entities (13)**:
1. `Order` - Core entity with optimistic concurrency (RowVersion)
2. `OrderStatus` - Status history with encrypted internal notes
3. `OrderFile` - File metadata with role classification
4. `OrderNote` - Customer/internal notes
5. `ServiceCategory` - 11 service categories (3D Printing, CNC, etc.)
6. `ProcessType` - 14 process types (FDM, DLP, Laser Cutting, etc.)
7. `AuditLog` - 7-year audit trail
8. `NotificationSubscription` - Order notification preferences
9. `Order3DPrintingAttributes` - 3D printing specifics
10. `OrderCncMachiningAttributes` - CNC specifics
11. `OrderSheetMetalAttributes` - Sheet metal specifics
12. `Order3DScanningAttributes` - 3D scanning specifics
13. `Order3DDesignAttributes` - 3D design specifics

**Key Features**:
- **Optimistic Concurrency**: RowVersion for conflict detection
- **Material Caching**: 24-hour TTL to reduce external service calls
- **Soft Delete**: 30-day retention for files
- **Audit Trail**: 7-year retention for compliance
- **Sequential IDs**: ORD-{YYYY}-{NNNNN} with yearly reset

**Naming Conventions**:
- Tables: PascalCase plural (e.g., `Orders`, `OrderStatuses`)
- Columns: PascalCase (e.g., `OrderId`, `CreatedAt`)
- Database: `order_app_db`

---

## External Service Integration

**7 External Services with Retry Policies** (3 attempts, exponential backoff):

### 1. Customer Service
- NDA validation (`HasActiveNdaAsync`)
- Customer details lookup (`GetCustomerDetailsAsync`)

### 2. Material Service
- Material/Color/Surface Finishing names (24-hour cache)
- `GetMaterialNameAsync`, `GetColorNameAsync`, `GetSurfaceFinishingNameAsync`

### 3. Payment Service
- Payment status tracking (`GetPaymentStatusAsync`)
- Partial charge calculation for cancellations (`CalculatePartialChargeAsync`)

### 4. Upload Service
- File upload/download (max 100MB per file, 5-minute timeout)
- GCS object path: `orders/{orderId}/files/{filename}`

### 5. Auth Service
- JWT token validation (`ValidateTokenAsync`)
- User context (userType, userId, roles, departmentId)

### 6. Employee Service
- Employee details (`GetEmployeeDetailsAsync`)
- Department listing (`GetDepartmentsAsync`)

### 7. Notification Service
- Multi-channel notifications (`SendOrderNotificationAsync`)
- Supports LINE, Email, SMS

---

## Testing

### Test Results
```
✅ Total Tests: 75
✅ Passed: 75 (100%)
❌ Failed: 0
⏭️  Skipped: 0
```

### Running Tests

```bash
# Run all tests
dotnet test Maliev.OrderService.sln --verbosity normal

# Run contract tests only
dotnet test Maliev.OrderService.Tests --filter "FullyQualifiedName~Contract"

# Run unit tests only
dotnet test Maliev.OrderService.Tests --filter "FullyQualifiedName~Unit"
```

### Test Infrastructure
- **Database**: Tests use actual PostgreSQL database to validate real database behavior including transactions, constraints, and RowVersion
- **PostgreSQL Required**: Local testing requires PostgreSQL 18 running on localhost:5432 or configure ConnectionStrings__OrderDbContext environment variable
- **Docker Compose**: Use `docker-compose.test.yml` for easy PostgreSQL setup (recommended for local development)
- **GitHub Actions**: CI/CD workflows include PostgreSQL service container for automated testing
- **Authentication**: TestAuthHandler provides mock authentication using standard ASP.NET Core Identity claims (ClaimTypes.NameIdentifier, ClaimTypes.Role, etc.)
- **External Services**: All external service clients are mocked for test isolation

---

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__OrderDbContext="Host=localhost;Port=5432;Database=order_app_db;User Id=postgres;Password=..."

# JWT Authentication
Jwt__SecurityKey="development-key-32-characters-minimum-required-for-hs256-algorithm"
Jwt__Issuer="maliev-dev"
Jwt__Audience="maliev-dev"

# External Services (7 services)
ExternalServices__CustomerService__BaseUrl="http://localhost:5001"
ExternalServices__CustomerService__TimeoutSeconds=180

ExternalServices__MaterialService__BaseUrl="http://localhost:5002"
ExternalServices__MaterialService__TimeoutSeconds=180

ExternalServices__PaymentService__BaseUrl="http://localhost:5003"
ExternalServices__PaymentService__TimeoutSeconds=180

ExternalServices__UploadService__BaseUrl="http://localhost:5004"
ExternalServices__UploadService__TimeoutSeconds=300  # 300 for file uploads

ExternalServices__AuthService__BaseUrl="http://localhost:5005"
ExternalServices__AuthService__TimeoutSeconds=180

ExternalServices__EmployeeService__BaseUrl="http://localhost:5006"
ExternalServices__EmployeeService__TimeoutSeconds=180

ExternalServices__NotificationService__BaseUrl="http://localhost:5007"
ExternalServices__NotificationService__TimeoutSeconds=180

# CORS Configuration (flat variable, comma-separated)
CORS_ALLOWED_ORIGINS="http://localhost:3000,http://localhost:3001"

# Redis
ConnectionStrings__Cache="redis:6379,password=..."

# RabbitMQ
RabbitMQ__Host="rabbitmq"
RabbitMQ__Username="guest"
RabbitMQ__Password="guest"
```

---

## Deployment

### Kubernetes (GKE)

**Namespaces:**
- Development: `maliev-dev`
- Staging: `maliev-staging`
- Production: `maliev-prod`

**Service URL:** `http://maliev-order-service:8080/order`

### Docker Image

```bash
# Image naming convention
asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-{env}/maliev-order-service:{sha}
```

---

## Support

For issues or questions:

- **GitHub Issues**: https://github.com/MALIEV-Co-Ltd/Maliev.OrderService/issues
- **Email**: dev@maliev.co.th
- **Documentation**: https://docs.maliev.co.th/order-service

---

## License

Copyright © 2025 Maliev Co. Ltd. All rights reserved.
