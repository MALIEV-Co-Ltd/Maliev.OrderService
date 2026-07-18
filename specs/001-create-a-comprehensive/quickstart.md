# Quickstart Guide: Order Service Integration Tests

**Feature**: Order Service API for Rapid Prototyping & Manufacturing
**Date**: 2025-10-02
**Phase**: Phase 1 - Design & Contracts

## Purpose

This document provides executable integration test scenarios that validate the complete Order Service functionality end-to-end. Each scenario exercises the full stack: API → Services → Database → External Service Integrations.

**Test Execution Order**: Run scenarios sequentially to ensure proper test data setup.

---

## Prerequisites

### Environment Setup

```bash
# 1. Start PostgreSQL database
docker run -d \
  --name order-service-db \
  -e POSTGRES_DB=order_app_db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=<dev-password> \
  -p 5432:5432 \
  postgres:18

# 2. Apply EF Core migrations
export ConnectionStrings__OrderDbContext="Server=localhost;Port=5432;Database=order_app_db;User Id=postgres;Password=<dev-password>;"
dotnet ef database update --project Maliev.OrderService.Data

# 3. Configure external service endpoints (dev environment)
export CUSTOMER_SERVICE_URL="https://dev.api.maliev.com/customers"
export MATERIAL_SERVICE_URL="https://dev.api.maliev.com/materials"
export PAYMENT_SERVICE_URL="https://dev.api.maliev.com/payments"
export UPLOAD_SERVICE_URL="https://dev.api.maliev.com/uploads"
export AUTH_SERVICE_URL="https://dev.api.maliev.com/auth"
export EMPLOYEE_SERVICE_URL="https://dev.api.maliev.com/employees"
export NOTIFICATION_SERVICE_URL="https://dev.api.maliev.com/notifications"
export CORS_ALLOWED_ORIGINS="https://dev.intranet.maliev.com,https://dev.www.maliev.com"

# 4. Start Order Service
dotnet run --project Maliev.OrderService.Api
# Service available at: http://localhost:5000/orders
```

### Test Data Setup

```bash
# Seed service categories and process types (auto-seeded via migration)
# Sample data:
# - ServiceCategory: 3D Printing (ID=1), CNC Machining (ID=5)
# - ProcessType: FDM (ID=1, CategoryId=1), 5-Axis Milling (ID=10, CategoryId=5)

# External service mock setup:
# - Customer Service: Customer CUST-001 exists, has NDA signed; CUST-002 exists, no NDA
# - Material Service: Material ID=1 (PLA), Color ID=1 (Black), Color ID=2 (White), SurfaceFinishing ID=1 (Smooth)
# - Employee Service: Employee EMP-001 exists, Department DEPT-001
# - Auth Service: Returns valid user context for test JWTs
```

---

## Scenario 1: Customer Creates Confidential Order (Auto NDA)

### Test Objective
Verify that orders are automatically marked confidential when customer has signed NDA agreement.

### Test Steps

```bash
# Step 1: Obtain customer JWT token (mock Auth Service)
export CUSTOMER_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." # CUST-001 (NDA signed)

# Step 2: Create order for 3D printing service
curl -X POST http://localhost:5000/orders/v1/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-001",
    "serviceCategoryId": 1,
    "processTypeId": 1,
    "requirements": "3D print prototype housing with FDM. ABS material, 0.2mm layer height."
  }'

# Expected Response: 201 Created
{
  "orderId": "ORD-2025-00001",
  "customerId": "CUST-001",
  "customerType": "Customer",
  "serviceCategoryId": 1,
  "serviceCategoryName": "3D Printing",
  "processTypeId": 1,
  "processTypeName": "FDM",
  "isConfidential": true,  # ✅ Automatic based on NDA
  "paymentId": null,
  "paymentStatus": "Unpaid",
  "currentStatus": "New",
  "version": "AAAAAAAAB9E=",
  "createdAt": "2025-10-02T10:00:00Z",
  "updatedAt": "2025-10-02T10:00:00Z",
  "createdBy": "CUST-001"
}
```

### Validation Checks

✅ **Customer Service Integration**: Order Service called `GET /customers/v1/CUST-001/nda` → returns `{ hasNda: true }`
✅ **Automatic Confidentiality**: `isConfidential` = `true` (not manually set)
✅ **Initial Status**: `currentStatus` = `New`
✅ **Audit Log Created**: AuditLog entry with `Action=OrderCreated`, `PerformedBy=CUST-001`
✅ **OrderId Format**: Matches pattern `ORD-YYYY-NNNNN`

---

## Scenario 2: Employee Updates Order Status with Dual Notes

### Test Objective
Verify status update with internal (employee-only) and customer-facing notes, plus notification trigger.

### Test Steps

```bash
# Step 1: Obtain employee JWT token
export EMPLOYEE_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." # EMP-001

# Step 2: Update status from New → Reviewing with dual notes
curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/statuses \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Reviewing",
    "internalNotes": "Waiting for material supplier response. Lead time 3-5 days.",
    "customerNotes": "Your order is being reviewed by our engineering team. We will provide a quote within 48 hours."
  }'

# Expected Response: 200 OK
{
  "statusId": 1,
  "orderId": "ORD-2025-00001",
  "status": "Reviewing",
  "internalNotes": "Waiting for material supplier response. Lead time 3-5 days.",
  "customerNotes": "Your order is being reviewed by our engineering team...",
  "timestamp": "2025-10-02T10:15:00Z",
  "updatedBy": "EMP-001"
}

# Step 3: Customer retrieves status history (internal notes should be filtered)
curl -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00001/statuses \
  -H "Authorization: Bearer $CUSTOMER_TOKEN"

# Expected Response: 200 OK
[
  {
    "statusId": 1,
    "orderId": "ORD-2025-00001",
    "status": "Reviewing",
    "internalNotes": null,  # ✅ Filtered for Customer role
    "customerNotes": "Your order is being reviewed by our engineering team...",
    "timestamp": "2025-10-02T10:15:00Z",
    "updatedBy": "EMP-001"
  }
]
```

### Validation Checks

✅ **Valid State Transition**: `New` → `Reviewing` allowed per ValidTransitions
✅ **Internal Notes Encrypted**: `OrderStatus.InternalNotes` encrypted at rest in database
✅ **Role-Based Filtering**: Customer sees `internalNotes: null`, Employee sees full content
✅ **Notification Triggered**: Notification Service called `POST /v1/send` with order update event
✅ **Audit Log Created**: AuditLog entry with `Action=StatusChanged`, `ChangeDetails` JSON

---

## Scenario 3: Batch Operation with All-or-Nothing Rollback

### Test Objective
Verify batch update fails completely if any single order has validation error (transaction rollback).

### Test Steps

```bash
# Setup: Create second order for batch test
curl -X POST http://localhost:5000/orders/v1/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-001",
    "serviceCategoryId": 5,
    "processTypeId": 10,
    "requirements": "CNC milling aluminum part"
  }'
# Returns: ORD-2025-00002

# Step 1: Attempt batch update with one invalid order ID
curl -X PUT http://localhost:5000/orders/v1/orders/batch \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "orderId": "ORD-2025-00001",
      "version": "AAAAAAAAB9E=",
      "assignedEmployeeId": "EMP-001",
      "departmentId": "DEPT-001"
    },
    {
      "orderId": "INVALID-ORDER",
      "version": "AAAAAAAAB9E=",
      "assignedEmployeeId": "EMP-001",
      "departmentId": "DEPT-001"
    }
  ]'

# Expected Response: 400 Bad Request
{
  "error": "Batch update failed - all changes rolled back",
  "failedItems": [
    {
      "index": 1,
      "item": {
        "orderId": "INVALID-ORDER",
        ...
      },
      "validationErrors": [
        "Order 'INVALID-ORDER' not found"
      ]
    }
  ]
}

# Step 2: Verify first order NOT updated (rollback successful)
curl -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00001 \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN"

# Expected: assignedEmployeeId is still null (no partial update)
{
  "orderId": "ORD-2025-00001",
  "assignedEmployeeId": null,  # ✅ Rollback successful
  ...
}
```

### Validation Checks

✅ **Transaction Rollback**: Database transaction rolled back on any failure
✅ **No Partial Updates**: First valid order NOT updated (all-or-nothing)
✅ **Detailed Error Response**: Returns index and validation errors for failed items
✅ **Database Consistency**: No orphaned records or partial state

---

## Scenario 4: File Upload with Retry and Size Validation

### Test Objective
Verify file upload integration with Upload Service, automatic retry on failure, and size limit enforcement.

### Test Steps

```bash
# Step 1: Upload CAD file (50MB STL)
curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/files \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -F "file=@prototype_housing.stl" \
  -F "accessLevel=Confidential"

# Expected Response: 201 Created
{
  "fileId": 1,
  "orderId": "ORD-2025-00001",
  "objectPath": "orders/ORD-2025-00001/files/prototype_housing.stl",
  "fileName": "prototype_housing.stl",
  "fileSize": 52428800,  # 50MB in bytes
  "fileType": "model/stl",
  "accessLevel": "Confidential",
  "uploadedAt": "2025-10-02T10:30:00Z",
  "uploadedBy": "CUST-001"
}

# Step 2: Attempt upload exceeding per-file limit (110MB file)
curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/files \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -F "file=@large_assembly.step" \
  -F "accessLevel=Internal"

# Expected Response: 400 Bad Request
{
  "error": "File size exceeds maximum limit",
  "details": "File size 115343360 bytes exceeds maximum 104857600 bytes (100MB)"
}

# Step 3: Upload additional files until total size limit reached
# Upload 5 more files (40MB each) → Total 250MB
# Then attempt 6th file (300MB more) → Should fail

curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/files \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -F "file=@reference_drawing.pdf"

# Expected Response: 400 Bad Request
{
  "error": "Total order file size exceeds maximum limit",
  "details": "Current total 262144000 bytes + new file 52428800 bytes exceeds maximum 524288000 bytes (500MB)"
}
```

### Validation Checks

✅ **Upload Service Integration**: Called `POST /uploads/v1/files` with multipart form data
✅ **ObjectPath Pattern**: Follows `orders/{orderId}/files/{filename}` format
✅ **Retry Policy**: Polly policy retries 3 times on transient HTTP errors (2s, 4s, 8s delays)
✅ **Per-File Size Limit**: Rejects files > 100MB
✅ **Total Size Limit**: Rejects when total order files > 500MB
✅ **File Metadata Stored**: Database record created with GCS object path reference

---

## Scenario 5: Order Cancellation with Partial Charge

### Test Objective
Verify order cancellation after work started, with refund calculation via Payment Service.

### Test Steps

```bash
# Setup: Advance order to InProgress status
curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/statuses \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "Reviewed"}'

curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/statuses \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "Quoted"}'

curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/statuses \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "Accepted"}'

# Simulate payment (update paymentId and status)
curl -X PUT http://localhost:5000/orders/v1/orders/ORD-2025-00001 \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "AAAAAAAAB9E=",
    "paymentId": "PAY-001",
    "paymentStatus": "Paid"
  }'

curl -X POST http://localhost:5000/orders/v1/orders/ORD-2025-00001/statuses \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status": "InProgress", "customerNotes": "Production started"}'

# Step 1: Cancel order with partial charge calculation
curl -X DELETE "http://localhost:5000/orders/v1/orders/ORD-2025-00001?calculatePartialCharge=true&reason=Customer%20changed%20requirements" \
  -H "Authorization: Bearer $CUSTOMER_TOKEN"

# Expected Response: 200 OK
{
  "orderId": "ORD-2025-00001",
  "cancelled": true,
  "reason": "Customer changed requirements",
  "refundAmount": 6000.00,  # 60% refund (work 40% complete)
  "partialChargeAmount": 4000.00  # 40% charged for work completed
}

# Step 2: Verify order status updated to Cancelled
curl -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00001 \
  -H "Authorization: Bearer $CUSTOMER_TOKEN"

# Expected: currentStatus = "Cancelled"
{
  "orderId": "ORD-2025-00001",
  "currentStatus": "Cancelled",
  ...
}
```

### Validation Checks

✅ **Valid Cancellation**: Allowed from `InProgress` status (per ValidTransitions)
✅ **Payment Service Integration**: Called `POST /payments/v1/refunds` with partial charge calculation
✅ **Refund Calculation**: Returns `refundAmount` and `partialChargeAmount` from Payment Service
✅ **Status Transition**: Order moved to `Cancelled` (terminal state)
✅ **Audit Trail**: AuditLog entry with `Action=OrderCancelled`, cancellation reason in `ChangeDetails`

---

## Scenario 6: Optimistic Concurrency Conflict Handling

### Test Objective
Verify version-based concurrency control with conflict detection and error response.

### Test Steps

```bash
# Step 1: Two employees retrieve same order simultaneously
# Employee 1:
export ORDER_V1=$(curl -s -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" | jq -r '.version')
echo "Employee 1 version: $ORDER_V1"  # AAAAAAAAB9E=

# Employee 2 (different token):
export EMPLOYEE2_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." # EMP-002
export ORDER_V2=$(curl -s -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE2_TOKEN" | jq -r '.version')
echo "Employee 2 version: $ORDER_V2"  # AAAAAAAAB9E= (same version)

# Step 2: Employee 1 updates order (succeeds)
curl -X PUT http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"version\": \"$ORDER_V1\",
    \"assignedEmployeeId\": \"EMP-001\",
    \"departmentId\": \"DEPT-001\"
  }"
# Response: 200 OK with new version "AAAAAAAAB+A="

# Step 3: Employee 2 attempts update with stale version (fails)
curl -X PUT http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE2_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"version\": \"$ORDER_V2\",
    \"assignedEmployeeId\": \"EMP-002\",
    \"departmentId\": \"DEPT-002\"
  }"

# Expected Response: 409 Conflict
{
  "error": "Concurrency conflict - order was modified by another user",
  "details": "Current version 'AAAAAAAAB+A=' does not match provided version 'AAAAAAAAB9E='. Please refresh and retry.",
  "traceId": "00-abc123..."
}

# Step 4: Employee 2 refreshes and retries
export ORDER_V3=$(curl -s -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE2_TOKEN" | jq -r '.version')
echo "New version: $ORDER_V3"  # AAAAAAAAB+A=

curl -X PUT http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE2_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"version\": \"$ORDER_V3\",
    \"assignedEmployeeId\": \"EMP-002\"
  }"
# Response: 200 OK (succeeds with correct version)
```

### Validation Checks

✅ **Version Validation**: Update rejected if `version` doesn't match current database value
✅ **409 Conflict Response**: Returns conflict status with helpful error message
✅ **EF Core Integration**: Uses `RowVersion` concurrency token (automatic increment)
✅ **Retry Pattern**: Client refreshes version and retries successfully
✅ **Atomic Update**: No lost updates due to concurrent modifications

---

## Scenario 7: RBAC Context-Based Authorization

### Test Objective
Verify role-based access control with context from Auth Service (Customer/Employee/Manager/Admin).

### Test Steps

```bash
# Step 1: Customer attempts to access another customer's order
export CUSTOMER2_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." # CUST-003

curl -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00001 \
  -H "Authorization: Bearer $CUSTOMER2_TOKEN"

# Expected Response: 403 Forbidden
{
  "error": "Access denied",
  "details": "Customer can only access their own orders"
}

# Step 2: Employee accesses order assigned to them (allowed)
curl -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00002 \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN"

# Expected: 200 OK (EMP-001 is assigned to ORD-2025-00002)

# Step 3: Employee attempts to access unassigned order (forbidden)
curl -X GET http://localhost:5000/orders/v1/orders/ORD-2025-00001 \
  -H "Authorization: Bearer $EMPLOYEE_TOKEN"

# Expected Response: 403 Forbidden
{
  "error": "Access denied",
  "details": "Employee can only access assigned orders"
}

# Step 4: Manager accesses all department orders (allowed)
export MANAGER_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." # MGR-001, DEPT-001

curl -X GET "http://localhost:5000/orders/v1/orders?departmentId=DEPT-001" \
  -H "Authorization: Bearer $MANAGER_TOKEN"

# Expected: 200 OK with all DEPT-001 orders

# Step 5: Admin accesses all orders including confidential (allowed)
export ADMIN_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." # ADMIN-001

curl -X GET "http://localhost:5000/orders/v1/orders?isConfidential=true" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Expected: 200 OK with all confidential orders
```

### Validation Checks

✅ **Auth Service Integration**: JWT validated via `POST /auth/v1/validate`, returns `UserContext`
✅ **Customer Scope**: Can only access `Order.CustomerId = UserContext.UserId`
✅ **Employee Scope**: Can only access `Order.AssignedEmployeeId = UserContext.UserId`
✅ **Manager Scope**: Can access `Order.DepartmentId = UserContext.DepartmentId`
✅ **Admin Scope**: Can access all orders (no restrictions)
✅ **Confidential Access Audit**: All confidential order access logged to AuditLog

---

## Test Summary Checklist

After running all scenarios, verify:

- [x] **FR-001-013**: Order CRUD operations (create, read, update, cancel, batch)
- [x] **FR-014-019**: File upload/download/delete with size limits and retry
- [x] **FR-020-027**: Status tracking with 16-state workflow and dual notes
- [x] **FR-028-032**: RBAC with Customer/Employee/Manager/Admin roles
- [x] **FR-033-042**: External service integrations (Customer, Payment, Upload, Auth, Employee, Notification)
- [x] **FR-043-047**: Notification triggers on status change
- [x] **Concurrency Control**: Optimistic locking with version conflict detection
- [x] **Batch Transactions**: All-or-nothing rollback on partial failure
- [x] **Audit Trail**: All operations logged to AuditLog table
- [x] **Data Retention**: 30-day file deletion, 7-year order retention

---

## Performance Benchmarks

**Target Metrics** (from research.md):
- Response time: <200ms p95
- Throughput: 500+ req/s
- Memory: <512MB per pod

**Benchmark Commands**:
```bash
# Load test with Apache Bench (1000 requests, 10 concurrent)
ab -n 1000 -c 10 -H "Authorization: Bearer $EMPLOYEE_TOKEN" \
  http://localhost:5000/orders/v1/orders

# Expected output:
# Requests per second: 550 [#/sec]
# Time per request (mean): 18ms
# Time per request (95th percentile): 180ms
```

---

*Quickstart scenarios complete. All integration tests executable end-to-end.*
