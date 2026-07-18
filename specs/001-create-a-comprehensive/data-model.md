# Data Model: Order Service

**Feature**: Order Service API for Rapid Prototyping & Manufacturing
**Date**: 2025-10-02
**Phase**: Phase 1 - Design & Contracts

## Overview

This document defines the complete data model for the Order Service, including all entities, relationships, validation rules, and state transition logic. The model is designed for PostgreSQL 18 with Entity Framework Core 9.0.9.

---

## Entity Relationship Diagram

```
┌─────────────────┐
│ ServiceCategory │
│─────────────────│
│ CategoryId (PK) │
│ Name            │
│ Description     │
│ IsActive        │
└────────┬────────┘
         │
         │ 1:N
         │
         ▼
    ┌─────────────┐      1:N       ┌─────────────┐
    │ ProcessType │◄───────────────┤    Order    │
    │─────────────│                │─────────────│
    │ ProcessId   │                │ OrderId (PK)│
    │ CategoryId  │                │ CustomerId  │
    │ Name        │                │ CategoryId  │
    │ Description │                │ ProcessId   │
    │ IsActive    │                │ IsConf...   │
    └─────────────┘                │ Version     │
                                   │ ...         │
                                   └──────┬──────┘
                                          │
                     ┌────────────────────┼────────────────────┐
                     │                    │                    │
                     │ 1:N                │ 1:N                │ 1:N
                     ▼                    ▼                    ▼
              ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
              │ OrderStatus │      │  OrderFile  │      │  AuditLog   │
              │─────────────│      │─────────────│      │─────────────│
              │ StatusId    │      │ FileId      │      │ AuditId     │
              │ OrderId     │      │ OrderId     │      │ OrderId     │
              │ Status      │      │ ObjectPath  │      │ Action      │
              │ InternalNotes│     │ FileName    │      │ PerformedBy │
              │ CustomerNotes│     │ FileSize    │      │ EntityType  │
              │ Timestamp   │      │ DeletedAt   │      │ EntityId    │
              │ UpdatedBy   │      │ ...         │      │ ...         │
              └─────────────┘      └─────────────┘      └─────────────┘

┌──────────────────────────┐
│ NotificationSubscription │
│──────────────────────────│
│ SubscriptionId (PK)      │
│ CustomerId               │
│ IsSubscribed             │
│ Channels (JSON)          │
└──────────────────────────┘
```

---

## Core Entities

### 1. Order

**Purpose**: Represents a customer order for manufacturing/prototyping services

**Table**: `orders`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `OrderId` | `varchar(50)` | `PK, NOT NULL` | Unique order identifier (e.g., `ORD-2025-00001`) |
| `CustomerId` | `varchar(50)` | `NOT NULL, INDEX` | Reference to Customer Service (external) |
| `CustomerType` | `varchar(20)` | `NOT NULL` | `Customer` or `Employee` (for RBAC) |
| `ServiceCategoryId` | `int` | `FK, NOT NULL` | Reference to `ServiceCategory` |
| `ProcessTypeId` | `int` | `FK, NULL, INDEX` | Reference to `ProcessType` (nullable - consulting/procurement may not have process) |
| `MaterialId` | `int` | `NULL, INDEX` | Reference to Material Service (external, nullable) |
| `ColorId` | `int` | `NULL` | Reference to Material Service (external, nullable) |
| `SurfaceFinishingId` | `int` | `NULL` | Reference to Material Service (external, nullable) |
| `MaterialName` | `varchar(100)` | `NULL` | Cached display name from Material Service (24-hour TTL) |
| `ColorName` | `varchar(100)` | `NULL` | Cached display name from Material Service (24-hour TTL) |
| `SurfaceFinishingName` | `varchar(100)` | `NULL` | Cached display name from Material Service (24-hour TTL) |
| `MaterialCacheUpdatedAt` | `timestamp` | `NULL` | Last cache refresh timestamp |
| `OrderedQuantity` | `int` | `NULL` | Total parts/units requested (nullable - manufacturing orders only) |
| `ManufacturedQuantity` | `int` | `NULL, DEFAULT 0` | Running total of completed units (nullable) |
| `RemainingQuantity` | `int` | `GENERATED ALWAYS AS (ordered_quantity - manufactured_quantity) STORED` | Computed column |
| `LeadTimeDays` | `int` | `NULL` | Expected days to complete order (nullable) |
| `PromisedDeliveryDate` | `date` | `NULL` | Date committed to customer (nullable) |
| `ActualDeliveryDate` | `date` | `NULL` | Date order actually shipped/delivered (nullable) |
| `QuotedAmount` | `decimal(10,2)` | `NULL` | Quote amount from Quoting Service (nullable) |
| `QuoteCurrency` | `varchar(10)` | `NULL, DEFAULT 'THB'` | Currency code (THB, USD, etc.) |
| `IsConfidential` | `boolean` | `NOT NULL, DEFAULT false` | Auto-set based on NDA status |
| `PaymentId` | `varchar(50)` | `NULL, INDEX` | Reference to Payment Service (external) |
| `PaymentStatus` | `varchar(20)` | `NOT NULL, DEFAULT 'Unpaid'` | `Unpaid`, `Paid`, `POIssued` |
| `AssignedEmployeeId` | `varchar(50)` | `NULL, INDEX` | Reference to Employee Service (external) |
| `DepartmentId` | `varchar(50)` | `NULL, INDEX` | Department assignment for Manager access |
| `Requirements` | `text` | `NULL` | Customer requirements description |
| `Version` | `bytea` | `NOT NULL (RowVersion)` | Optimistic concurrency control |
| `CreatedAt` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |
| `UpdatedAt` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP` | Last update timestamp |
| `CreatedBy` | `varchar(50)` | `NOT NULL` | UserId from Auth Service |
| `UpdatedBy` | `varchar(50)` | `NOT NULL` | UserId from Auth Service |

**Indexes**:
- `IX_Order_CustomerId` (for customer order lookup)
- `IX_Order_AssignedEmployeeId` (for employee assigned orders)
- `IX_Order_DepartmentId` (for manager department access)
- `IX_Order_PaymentId` (for payment reference lookup)
- `IX_Order_MaterialId` (for material-based queries)
- `IX_Order_ProcessTypeId` (for process type filtering, allows NULL)
- `IX_Order_CreatedAt` (for date range queries)

**Validation Rules**:
- `OrderId` must match pattern `^ORD-\d{4}-\d{5}$` (e.g., `ORD-2025-00001`)
- `CustomerId` must exist in Customer Service API (validated on create)
- `ServiceCategoryId` must be active
- `ProcessTypeId` (if provided) must be active and belong to the selected `ServiceCategoryId`
- `MaterialId`, `ColorId`, `SurfaceFinishingId` (if provided) must be validated via Material Service API
- `CustomerType` must be `Customer` or `Employee`
- `PaymentStatus` must be `Unpaid`, `Paid`, or `POIssued`
- `OrderedQuantity` and `ManufacturedQuantity` must be ≥ 0 if provided
- `ManufacturedQuantity` must be ≤ `OrderedQuantity`
- `LeadTimeDays` must be ≥ 0 if provided
- `PromisedDeliveryDate` must be ≥ `CreatedAt` if provided
- `QuotedAmount` must be ≥ 0 if provided

**Business Rules**:
- If customer has NDA agreement, `IsConfidential` = `true` (automatic, not manually settable)
- `PaymentId` can only be set when `PaymentStatus` = `Paid` or `POIssued`
- `AssignedEmployeeId` must exist in Employee Service (validated on assignment)
- `ProcessTypeId` nullable for orders that don't fit standard processes (consulting, procurement, custom services)
- Material fields nullable for non-manufacturing orders (consulting, design without material selection)
- Quantity fields nullable for non-manufacturing orders (design services, consulting, scanning)
- `RemainingQuantity` auto-computed: `ordered_quantity - manufactured_quantity`
- Material cache refreshed when `MaterialCacheUpdatedAt` > 24 hours old
- `UpdatedAt` auto-updates on any modification (EF Core `[ConcurrencyCheck]`)

---

### 2. OrderStatus

**Purpose**: Tracks order status history and notes

**Table**: `order_statuses`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `StatusId` | `bigserial` | `PK, AUTO_INCREMENT` | Unique status entry ID |
| `OrderId` | `varchar(50)` | `FK, NOT NULL, INDEX` | Reference to `Order` |
| `Status` | `varchar(20)` | `NOT NULL, INDEX` | Current status (enum) |
| `InternalNotes` | `text` | `NULL (encrypted)` | Employee-only notes |
| `CustomerNotes` | `text` | `NULL` | Customer-visible notes |
| `Timestamp` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP` | Status change time |
| `UpdatedBy` | `varchar(50)` | `NOT NULL` | Employee UserId |

**Indexes**:
- `IX_OrderStatus_OrderId` (for status history lookup)
- `IX_OrderStatus_Status` (for status-based filtering)
- `IX_OrderStatus_Timestamp` (for chronological sorting)

**Status Enum Values**:
- **Primary Flow**: `New`, `Reviewing`, `Rejected`, `Reviewed`, `Quoted`, `Declined`, `Accepted`, `Expired`, `Paid`, `POIssued`, `InProgress`, `Finished`, `Shipped`
- **Exception Flow**: `OnHold`, `Reopen`, `Cancelled`

**Valid State Transitions**:

| From Status | To Status | Notes |
|-------------|-----------|-------|
| `New` | `Reviewing`, `Cancelled` | Initial review or immediate cancel |
| `Reviewing` | `Rejected`, `Reviewed`, `Cancelled` | Review decision or cancel |
| `Rejected` | *(none)* | Terminal state |
| `Reviewed` | `Quoted`, `Cancelled` | Generate quote or cancel |
| `Quoted` | `Declined`, `Accepted`, `Expired`, `Cancelled` | Quote response or timeout |
| `Declined` | *(none)* | Terminal state |
| `Expired` | *(none)* | Terminal state |
| `Accepted` | `Paid`, `POIssued`, `Cancelled` | Payment method or cancel |
| `Paid` | `InProgress`, `Cancelled` | Start work or cancel |
| `POIssued` | `InProgress`, `Cancelled` | Start work or cancel |
| `InProgress` | `OnHold`, `Finished`, `Cancelled` | Pause, complete, or cancel |
| `OnHold` | `InProgress`, `Cancelled` | Resume or cancel |
| `Finished` | `Shipped`, `Reopen` | Deliver or rework |
| `Shipped` | `Reopen` | Customer requests rework |
| `Reopen` | `InProgress` | Resume work |
| `Cancelled` | *(none)* | Terminal state |

**Validation Rules**:
- Transition must be in `ValidTransitions` dictionary (enforced by business logic)
- `UpdatedBy` must be Employee, Manager, or Admin role (Customer cannot update status)
- `InternalNotes` encrypted at rest (EF Core `[Encrypted]` attribute)
- `CustomerNotes` visible to all authenticated users with order access

**Business Rules**:
- Each status change creates a new `OrderStatus` record (append-only history)
- Latest status determines current `Order.CurrentStatus` (computed property)
- Notification triggered on status change if customer subscribed

---

### 3. OrderFile

**Purpose**: Tracks file metadata for order attachments

**Table**: `order_files`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `FileId` | `bigserial` | `PK, AUTO_INCREMENT` | Unique file ID |
| `OrderId` | `varchar(50)` | `FK, NOT NULL, INDEX` | Reference to `Order` |
| `FileRole` | `varchar(20)` | `NOT NULL, INDEX` | `Input`, `Output`, or `Supporting` |
| `FileCategory` | `varchar(20)` | `NOT NULL, INDEX` | `CAD`, `Drawing`, `Image`, `Document`, `Archive`, `Other` |
| `DesignUnits` | `varchar(10)` | `NULL` | `mm`, `inch`, `cm`, `m` (nullable, CAD files only) |
| `ObjectPath` | `varchar(500)` | `NOT NULL, UNIQUE` | GCS object path |
| `FileName` | `varchar(255)` | `NOT NULL` | Original filename |
| `FileSize` | `bigint` | `NOT NULL` | Size in bytes (max 100MB) |
| `FileType` | `varchar(50)` | `NOT NULL` | MIME type or extension |
| `AccessLevel` | `varchar(20)` | `NOT NULL, DEFAULT 'Internal'` | `Internal` or `Confidential` |
| `UploadedAt` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP` | Upload timestamp |
| `UploadedBy` | `varchar(50)` | `NOT NULL` | UserId from Auth Service |
| `DeletedAt` | `timestamp` | `NULL` | Soft delete timestamp (30-day retention) |

**Indexes**:
- `IX_OrderFile_OrderId` (for order file listing)
- `IX_OrderFile_FileRole` (for filtering by role: Input/Output/Supporting)
- `IX_OrderFile_FileCategory` (for filtering by category: CAD/Drawing/etc.)
- `IX_OrderFile_ObjectPath` (for file lookup by path)
- `IX_OrderFile_DeletedAt` (for cleanup query: `WHERE DeletedAt IS NULL`)

**FileRole Enum Values**:
- `Input`: Files provided by customer (CAD files, sketches, reference images, drawings)
- `Output`: Files delivered by MALIEV (finished CAD models, 3D scan data, deviation reports)
- `Supporting`: Other documentation (quotes, specifications, process notes, invoices)

**FileCategory Enum Values**:
- `CAD`: STL, STEP, IGES, OBJ, SolidWorks files, 3MF, STP
- `Drawing`: 2D technical drawings (PDF, DWG, DXF)
- `Image`: Photos, reference images (JPG, PNG)
- `Document`: PDF documents, Word files, specifications
- `Archive`: ZIP, RAR, 7Z compressed files
- `Other`: Miscellaneous files

**Validation Rules**:
- `FileSize` ≤ 100MB (104,857,600 bytes) per file
- Total file size per order ≤ 500MB (524,288,000 bytes)
- `FileType` must be in supported types list
- `ObjectPath` format: `orders/{orderId}/files/{filename}` (consistent pattern)
- `AccessLevel` must be `Internal` or `Confidential`
- `FileRole` must be `Input`, `Output`, or `Supporting`
- `FileCategory` must be `CAD`, `Drawing`, `Image`, `Document`, `Archive`, or `Other`
- `DesignUnits` required for CAD files, must be null for non-CAD files
- `DesignUnits` (if provided) must be `mm`, `inch`, `cm`, or `m`

**Business Rules**:
- Files uploaded via Upload Service (not stored in Order Service DB)
- `ObjectPath` references GCS bucket location
- Soft delete: `DeletedAt` set 30 days after order completion (terminal state)
- Hard delete from GCS after soft delete (Background Service cleanup)
- Confidential files require `IsConfidential=true` on parent order
- `DesignUnits` nullable because one order can have files in different units
- Manufacturing workflow: Filter `FileRole=Input AND FileCategory=CAD` to identify files to manufacture
- Customer downloads: Filter `FileRole=Output` to show deliverables

---

### 4. ServiceCategory

**Purpose**: Defines available service categories

**Table**: `service_categories`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `CategoryId` | `serial` | `PK, AUTO_INCREMENT` | Unique category ID |
| `Name` | `varchar(100)` | `NOT NULL, UNIQUE` | Category name |
| `Description` | `text` | `NULL` | Category description |
| `IsActive` | `boolean` | `NOT NULL, DEFAULT true` | Active/inactive flag |

**Seed Data** (from spec):
1. 3D Printing
2. 3D Scanning
3. 3D Design Services
4. Reverse Engineering
5. CNC Machining
6. Silicone Mold Making
7. Casting Services
8. Injection Molding Services
9. Surface Finishing Services
10. Wire Cut
11. EDM (Electrical Discharge Machining)

**Validation Rules**:
- `Name` must be unique (case-insensitive)
- Cannot delete category with active orders (referential integrity)
- Only active categories selectable in order creation

---

### 5. ProcessType

**Purpose**: Defines specific processes within service categories

**Table**: `process_types`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `ProcessTypeId` | `serial` | `PK, AUTO_INCREMENT` | Unique process type ID |
| `ServiceCategoryId` | `int` | `FK, NOT NULL` | Reference to `ServiceCategory` |
| `Name` | `varchar(100)` | `NOT NULL` | Process name |
| `Description` | `text` | `NULL` | Process description |
| `IsActive` | `boolean` | `NOT NULL, DEFAULT true` | Active/inactive flag |

**Seed Data** (from spec):

**3D Printing** (CategoryId=1):
- FDM (Fused Deposition Modeling)
- DLP (Digital Light Processing)
- SLA (Stereolithography)
- DMLS (Direct Metal Laser Sintering)
- SLS (Selective Laser Sintering)
- MJF (Multi Jet Fusion)

**3D Scanning** (CategoryId=2):
- Structured Light
- Laser Scanning
- On-site Scanning
- In-house Scanning

**CNC Machining** (CategoryId=5):
- 3-Axis Milling
- 4-Axis Milling
- 5-Axis Milling
- CNC Turning (Lathe)

**Validation Rules**:
- Unique constraint on (`ServiceCategoryId`, `Name`)
- Cannot delete process type with active orders
- Only active process types selectable in order creation

---

### 6. AuditLog

**Purpose**: Immutable audit trail for all order operations

**Table**: `audit_logs`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `AuditId` | `bigserial` | `PK, AUTO_INCREMENT` | Unique audit entry ID |
| `OrderId` | `varchar(50)` | `NOT NULL, INDEX` | Reference to `Order` |
| `Action` | `varchar(50)` | `NOT NULL, INDEX` | Action type (enum) |
| `PerformedBy` | `varchar(50)` | `NOT NULL, INDEX` | UserId from Auth Service |
| `PerformedAt` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP, INDEX` | Action timestamp |
| `EntityType` | `varchar(50)` | `NOT NULL` | Entity affected |
| `EntityId` | `varchar(100)` | `NOT NULL` | ID of affected entity |
| `ChangeDetails` | `jsonb` | `NULL` | Before/after state (JSON) |

**Indexes**:
- `IX_AuditLog_OrderId` (for order audit history)
- `IX_AuditLog_PerformedBy` (for user action history)
- `IX_AuditLog_PerformedAt` (for time-based queries)
- `IX_AuditLog_Action` (for action type filtering)

**Action Enum Values**:
- `OrderCreated`, `OrderUpdated`, `OrderCancelled`
- `StatusChanged`
- `FileUploaded`, `FileDeleted`
- `AssignmentChanged`, `PaymentUpdated`

**ChangeDetails JSON Schema**:
```json
{
  "before": { /* entity state before change */ },
  "after": { /* entity state after change */ },
  "metadata": {
    "ipAddress": "string",
    "userAgent": "string",
    "reason": "string" // for cancellations
  }
}
```

**Business Rules**:
- **Append-only**: No updates or deletes allowed (immutable audit trail)
- 7-year retention policy (spec FR-040)
- All sensitive operations must create audit log entry
- Confidential order access creates audit log entry (FR-032)

---

### 7. NotificationSubscription

**Purpose**: Tracks customer notification preferences

**Table**: `notification_subscriptions`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `SubscriptionId` | `serial` | `PK, AUTO_INCREMENT` | Unique subscription ID |
| `CustomerId` | `varchar(50)` | `NOT NULL, UNIQUE` | Reference to Customer Service |
| `IsSubscribed` | `boolean` | `NOT NULL, DEFAULT true` | Opt-in status |
| `Channels` | `jsonb` | `NOT NULL, DEFAULT '[]'` | Array of channels (JSON) |
| `UpdatedAt` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP` | Last update timestamp |

**Channels JSON Example**:
```json
["LINE", "Email"]
```

**Supported Channels** (from spec):
- `LINE` - LINE chat notification
- `Email` - Email notification
- (Extensible for future channels: SMS, Push, etc.)

**Validation Rules**:
- `CustomerId` must exist in Customer Service
- `Channels` must be valid JSON array
- Each channel value must be in supported channels list

**Business Rules**:
- Notification triggered only if `IsSubscribed = true`
- Delivery delegated to Notification Service (not Order Service responsibility)
- Customers can update subscription via API

---

### 8. OrderNote

**Purpose**: Tracks order notes with full history (separate from status notes)

**Table**: `order_notes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `NoteId` | `bigserial` | `PK, AUTO_INCREMENT` | Unique note ID |
| `OrderId` | `varchar(50)` | `FK, NOT NULL, INDEX` | Reference to `Order` |
| `NoteType` | `varchar(20)` | `NOT NULL, INDEX` | `customer` or `internal` |
| `NoteText` | `text` | `NOT NULL` | Note content |
| `CreatedBy` | `varchar(50)` | `NOT NULL, INDEX` | UserId from Auth Service |
| `CreatedAt` | `timestamp` | `NOT NULL, DEFAULT CURRENT_TIMESTAMP, INDEX` | Creation timestamp |

**Indexes**:
- `IX_OrderNote_OrderId` (for order notes listing)
- `IX_OrderNote_NoteType` (for filtering by type)
- `IX_OrderNote_CreatedBy` (for user note history)
- `IX_OrderNote_CreatedAt` (for chronological sorting)

**NoteType Enum Values**:
- `customer`: Visible to all users (customers and employees) - for customer requirements, preferences, special requests
- `internal`: Visible only to employees (Employee, Manager, Admin roles) - for internal coordination ("VIP customer", "material delayed", "rush order")

**Validation Rules**:
- `NoteType` must be `customer` or `internal`
- `NoteText` must not be empty (max 10,000 characters)
- `OrderId` must reference existing order

**Business Rules**:
- Separate from status notes (OrderStatus.InternalNotes and OrderStatus.CustomerNotes)
- Status notes tied to specific status changes, OrderNotes are general coordination notes
- Customers can only create `customer` type notes
- Employees can create both `customer` and `internal` type notes
- API filters internal notes from customer responses
- Full history preserved (append-only, no updates/deletes)
- Retained for 7 years with order metadata

---

### 9. Order3DPrintingAttributes

**Purpose**: Service-specific attributes for 3D printing orders

**Table**: `order_3d_printing_attributes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `OrderId` | `varchar(50)` | `PK, FK` | Reference to `Order` (one-to-one) |
| `ThreadTapRequired` | `boolean` | `NOT NULL, DEFAULT false` | Post-processing: threading/tapping needed |
| `InsertRequired` | `boolean` | `NOT NULL, DEFAULT false` | Post-processing: inserts installation |
| `PartMarking` | `varchar(100)` | `NULL` | Part marking/engraving text |
| `PartAssemblyTestRequired` | `boolean` | `NOT NULL, DEFAULT false` | Assembly/fit testing required |

**Validation Rules**:
- `OrderId` must reference existing order with `ServiceCategoryId` = 3D Printing
- One-to-one relationship with Order (single attributes record per order)

**Business Rules**:
- Created when order service category is 3D Printing
- Nullable for other service types
- Indexed on `OrderId` for fast lookup

---

### 10. OrderCncMachiningAttributes

**Purpose**: Service-specific attributes for CNC machining orders

**Table**: `order_cnc_machining_attributes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `OrderId` | `varchar(50)` | `PK, FK` | Reference to `Order` (one-to-one) |
| `TapRequired` | `boolean` | `NOT NULL, DEFAULT false` | Threading/tapping required |
| `Tolerance` | `varchar(50)` | `NULL, INDEX` | Tolerance spec (e.g., "ISO 2768-1 ±0.125mm") |
| `SurfaceRoughness` | `varchar(20)` | `NULL` | Surface roughness (e.g., "3.2µm Ra") |
| `InspectionType` | `varchar(50)` | `NULL` | Inspection type (e.g., "cmm-formal-report") |

**Indexes**:
- `IX_OrderCncMachiningAttributes_Tolerance` (for tolerance-based queries: "all orders with ±0.05mm")

**Validation Rules**:
- `OrderId` must reference existing order with `ServiceCategoryId` = CNC Machining
- One-to-one relationship with Order

**Business Rules**:
- Created when order service category is CNC Machining
- Tolerance indexed for common query: "all tight-tolerance orders"

---

### 11. OrderSheetMetalAttributes

**Purpose**: Service-specific attributes for sheet metal orders

**Table**: `order_sheet_metal_attributes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `OrderId` | `varchar(50)` | `PK, FK` | Reference to `Order` (one-to-one) |
| `Thickness` | `varchar(20)` | `NULL` | Sheet thickness (e.g., "1.5mm") |
| `WeldingRequired` | `boolean` | `NOT NULL, DEFAULT false` | Welding required |
| `WeldingDetails` | `text` | `NULL` | Welding specifications/notes |
| `Tolerance` | `varchar(50)` | `NULL` | Tolerance spec |
| `InspectionType` | `varchar(50)` | `NULL` | Inspection type |

**Validation Rules**:
- `OrderId` must reference existing order with `ServiceCategoryId` = Sheet Metal
- One-to-one relationship with Order
- `WeldingDetails` required if `WeldingRequired = true`

**Business Rules**:
- Created when order service category is Sheet Metal
- `WeldingDetails` nullable when `WeldingRequired = false`

---

### 12. Order3DScanningAttributes

**Purpose**: Service-specific attributes for 3D scanning orders

**Table**: `order_3d_scanning_attributes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `OrderId` | `varchar(50)` | `PK, FK` | Reference to `Order` (one-to-one) |
| `RequiredAccuracy` | `varchar(20)` | `NULL` | Accuracy spec (e.g., "±0.1mm") |
| `ScanLocation` | `text` | `NULL` | Scan location (null = in-house, or customer address) |
| `OutputFileFormats` | `varchar(100)` | `NULL` | CSV list of formats (e.g., "STL,STEP,PLY") |
| `DeviationReportRequested` | `boolean` | `NOT NULL, DEFAULT false` | Deviation analysis report required |

**Validation Rules**:
- `OrderId` must reference existing order with `ServiceCategoryId` = 3D Scanning
- One-to-one relationship with Order
- `OutputFileFormats` stored as CSV string (e.g., "STL,STEP,PLY")

**Business Rules**:
- Created when order service category is 3D Scanning
- `ScanLocation` null = in-house scanning, text value = on-site address
- Output formats parsed from CSV for API responses

---

### 13. Order3DDesignAttributes

**Purpose**: Service-specific attributes for 3D design orders

**Table**: `order_3d_design_attributes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `OrderId` | `varchar(50)` | `PK, FK` | Reference to `Order` (one-to-one) |
| `ComplexityLevel` | `varchar(20)` | `NULL` | Complexity: `Simple`, `Medium`, `Complex` |
| `Deliverables` | `varchar(200)` | `NULL` | CSV list of deliverables |
| `DesignSoftware` | `varchar(50)` | `NULL` | Preferred software (e.g., "SolidWorks") |
| `RevisionRounds` | `int` | `NOT NULL, DEFAULT 2` | Number of revision rounds included |

**Validation Rules**:
- `OrderId` must reference existing order with `ServiceCategoryId` = 3D Design
- One-to-one relationship with Order
- `ComplexityLevel` must be `Simple`, `Medium`, or `Complex` if provided
- `RevisionRounds` must be ≥ 0

**Business Rules**:
- Created when order service category is 3D Design
- `ComplexityLevel` affects quoting (handled by Quoting Service, not Order Service)
- Default 2 revision rounds, customizable per order

---

## Relationships Summary

### One-to-Many Relationships

1. **ServiceCategory → ProcessType** (1:N)
   - One category has many process types
   - Foreign key: `ProcessType.ServiceCategoryId`
   - Delete behavior: Restrict (cannot delete category with processes)

2. **ServiceCategory → Order** (1:N)
   - One category has many orders
   - Foreign key: `Order.ServiceCategoryId`
   - Delete behavior: Restrict (cannot delete category with active orders)

3. **ProcessType → Order** (1:N)
   - One process type has many orders
   - Foreign key: `Order.ProcessTypeId`
   - Delete behavior: Restrict (cannot delete process with active orders)

4. **Order → OrderStatus** (1:N)
   - One order has many status history entries
   - Foreign key: `OrderStatus.OrderId`
   - Delete behavior: Cascade (delete statuses when order deleted)

5. **Order → OrderFile** (1:N)
   - One order has many files
   - Foreign key: `OrderFile.OrderId`
   - Delete behavior: Cascade (delete file records when order deleted)

6. **Order → AuditLog** (1:N)
   - One order has many audit entries
   - Foreign key: `AuditLog.OrderId`
   - Delete behavior: Restrict (preserve audit trail even if order deleted)

7. **Order → OrderNote** (1:N)
   - One order has many notes
   - Foreign key: `OrderNote.OrderId`
   - Delete behavior: Cascade (delete notes when order deleted, preserved in audit trail)

### One-to-One Relationships

1. **Order → Order3DPrintingAttributes** (1:1)
   - One order has one 3D printing attributes record (if service category = 3D Printing)
   - Foreign key: `Order3DPrintingAttributes.OrderId`
   - Delete behavior: Cascade

2. **Order → OrderCncMachiningAttributes** (1:1)
   - One order has one CNC machining attributes record (if service category = CNC Machining)
   - Foreign key: `OrderCncMachiningAttributes.OrderId`
   - Delete behavior: Cascade

3. **Order → OrderSheetMetalAttributes** (1:1)
   - One order has one sheet metal attributes record (if service category = Sheet Metal)
   - Foreign key: `OrderSheetMetalAttributes.OrderId`
   - Delete behavior: Cascade

4. **Order → Order3DScanningAttributes** (1:1)
   - One order has one 3D scanning attributes record (if service category = 3D Scanning)
   - Foreign key: `Order3DScanningAttributes.OrderId`
   - Delete behavior: Cascade

5. **Order → Order3DDesignAttributes** (1:1)
   - One order has one 3D design attributes record (if service category = 3D Design)
   - Foreign key: `Order3DDesignAttributes.OrderId`
   - Delete behavior: Cascade

### External References (No Foreign Keys)

- `Order.CustomerId` → Customer Service API
- `Order.MaterialId` → Material Service API
- `Order.ColorId` → Material Service API
- `Order.SurfaceFinishingId` → Material Service API
- `Order.PaymentId` → Payment Service API
- `Order.AssignedEmployeeId` → Employee Service API
- `Order.DepartmentId` → Employee Service API (department lookup)
- `NotificationSubscription.CustomerId` → Customer Service API

---

## Computed Properties (Not Stored)

### Order.CurrentStatus
```csharp
public OrderStatusEnum CurrentStatus
{
    get => OrderStatuses.OrderByDescending(s => s.Timestamp).FirstOrDefault()?.Status ?? OrderStatusEnum.New;
}
```

### Order.TotalFileSize
```csharp
public long TotalFileSize
{
    get => OrderFiles.Where(f => f.DeletedAt == null).Sum(f => f.FileSize);
}
```

### Order.IsTerminalState
```csharp
public bool IsTerminalState
{
    get
    {
        var terminalStates = new[] { OrderStatusEnum.Rejected, OrderStatusEnum.Declined,
                                     OrderStatusEnum.Expired, OrderStatusEnum.Cancelled,
                                     OrderStatusEnum.Shipped };
        return terminalStates.Contains(CurrentStatus);
    }
}
```

---

## Validation Rules Summary

### Order Creation Validation
1. ✅ Customer must exist in Customer Service API
2. ✅ Customer Service API must be available (FR-038)
3. ✅ NDA status must be retrieved and applied (FR-036, FR-037)
4. ✅ ServiceCategory and ProcessType must be active
5. ✅ ProcessType must belong to selected ServiceCategory
6. ✅ Initial status must be `New`

### File Upload Validation
1. ✅ File size ≤ 100MB (FR-018)
2. ✅ Total order files ≤ 500MB (FR-018)
3. ✅ File type in supported formats (FR-017)
4. ✅ ObjectPath follows pattern: `orders/{orderId}/files/{filename}`
5. ✅ Upload Service must be available (with retry - FR-019)

### Status Update Validation
1. ✅ Transition must be valid per ValidTransitions dictionary (FR-020)
2. ✅ User must be Employee, Manager, or Admin (Customer cannot update status)
3. ✅ Notification triggered if customer subscribed (FR-044)
4. ✅ Audit log entry created (FR-032)

### Batch Operation Validation
1. ✅ All items validated before transaction begins
2. ✅ Single failure rolls back entire batch (FR-008)
3. ✅ Return detailed error for failed items
4. ✅ Concurrency version checked for updates (FR-009, FR-010)

---

## Data Retention Policies

### 7-Year Retention (FR-040)
- **Order** records (all columns)
- **OrderStatus** history (all entries)
- **AuditLog** entries (complete audit trail)
- **NotificationSubscription** preferences

### 30-Day Retention After Completion (FR-041)
- **OrderFile** soft delete: `DeletedAt` set 30 days after order reaches terminal state
- Hard delete from GCS: Background Service cleanup after soft delete
- File metadata preserved in database (for audit trail)

### No Retention Limit
- **ServiceCategory** (reference data)
- **ProcessType** (reference data)

---

## Security & Encryption

### Column-Level Encryption
- `OrderStatus.InternalNotes` - Encrypted at rest (employee-only visibility)
- Encryption via EF Core `[Encrypted]` attribute (AES-256)

### Access Control
- **Customer Role**: Access only `Order.CustomerId = UserContext.UserId`
- **Employee Role**: Access only `Order.AssignedEmployeeId = UserContext.UserId`
- **Manager Role**: Access only `Order.DepartmentId = UserContext.DepartmentId`
- **Admin Role**: Access all orders

### Audit Requirements (FR-032)
- All access to confidential orders logged to `AuditLog`
- Include `PerformedBy` (UserId), `PerformedAt` (timestamp), `Action` (type)

---

## Database Indexes Strategy

### Performance Indexes
```sql
-- Order lookups
CREATE INDEX IX_Order_CustomerId ON orders(customer_id);
CREATE INDEX IX_Order_AssignedEmployeeId ON orders(assigned_employee_id);
CREATE INDEX IX_Order_DepartmentId ON orders(department_id);
CREATE INDEX IX_Order_CreatedAt ON orders(created_at DESC);

-- Status history
CREATE INDEX IX_OrderStatus_OrderId ON order_statuses(order_id);
CREATE INDEX IX_OrderStatus_Timestamp ON order_statuses(timestamp DESC);

-- File operations
CREATE INDEX IX_OrderFile_OrderId ON order_files(order_id);
CREATE INDEX IX_OrderFile_ObjectPath ON order_files(object_path);
CREATE INDEX IX_OrderFile_DeletedAt ON order_files(deleted_at) WHERE deleted_at IS NULL;

-- Audit queries
CREATE INDEX IX_AuditLog_OrderId ON audit_logs(order_id);
CREATE INDEX IX_AuditLog_PerformedBy ON audit_logs(performed_by);
CREATE INDEX IX_AuditLog_PerformedAt ON audit_logs(performed_at DESC);
```

---

## Migration Strategy

### Initial Migration (Create Tables)
```bash
dotnet ef migrations add InitialCreate --project Maliev.OrderService.Data
dotnet ef database update --project Maliev.OrderService.Data
```

### Seed Data Migration (Reference Data)
```bash
dotnet ef migrations add SeedServiceCategoriesAndProcessTypes --project Maliev.OrderService.Data
```

**Seed Script** (in migration `Up()` method):
```csharp
migrationBuilder.InsertData(
    table: "service_categories",
    columns: new[] { "name", "description", "is_active" },
    values: new object[,]
    {
        { "3D Printing", "Additive manufacturing services", true },
        { "3D Scanning", "3D digitization services", true },
        // ... 11 total categories
    });

migrationBuilder.InsertData(
    table: "process_types",
    columns: new[] { "service_category_id", "name", "description", "is_active" },
    values: new object[,]
    {
        { 1, "FDM", "Fused Deposition Modeling", true },
        { 1, "SLA", "Stereolithography", true },
        // ... all process types
    });
```

---

## Next Steps

With data model complete, Phase 1 continues with:
1. ✅ OpenAPI contract generation (`contracts/*.yaml`)
2. ✅ Failing contract tests (`Tests/Contract/`)
3. ✅ Quickstart integration scenarios (`quickstart.md`)
4. ✅ Update agent context file (`CLAUDE.md`)

All entities will be implemented using EF Core Fluent API configurations for explicit schema control.

---

*Data model design complete. Ready for contract generation.*
