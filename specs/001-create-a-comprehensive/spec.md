# Feature Specification: Order Service API for Rapid Prototyping & Manufacturing

**Feature Branch**: `001-create-a-comprehensive`
**Created**: 2025-10-02
**Status**: Draft
**Input**: User description: "Create a comprehensive Order Service API for rapid prototyping and manufacturing services (www.maliev.com). The business sells items on Shopify (injection molding machine, materials, 3D printed parts, etc.). MALIEV also offers services such as 3D Printing (FDM, DLP, SLA, DMLS, SLS, MJF, etc), 3D Scanning (Structured Light, Laser, on-site, in-house, etc.), 3D Design services, Reverse engineering services, CNC machining services (mill, turn, 4-5 axis), Silicone mold making services, casting services, injection molding services, surface finishing services, wire cut, edm, etc. Basically covers many aspects of rapid prototyping business and custom manufacturing services. Customer must be able to track order statues, employees must be able to assign/update order statuses. Order must have an ability to upload files related to order. There must be an option to mark the order as confidential (non-disclosure)."

---

## ⚡ Quick Guidelines
- ✅ Focus on WHAT users need and WHY
- ❌ Avoid HOW to implement (no tech stack, APIs, code structure)
- 👥 Written for business stakeholders, not developers

---

## Clarifications

### Session 2025-10-02
- **Q: What is the complete order status workflow?** → **A: Primary Flow: New → Reviewing → [Rejected (END) OR Reviewed] → Quoted → [Declined (END) OR Accepted OR Expired (END)] → [Paid OR PO Issued] → In Progress → Finished → Shipped. Exception Flows: In Progress ↔ On Hold (pause/resume), Finished/Shipped → Reopen → In Progress (rework), Paid → Cancelled (END). Terminal states: Rejected, Declined, Expired, Cancelled, Shipped (unless Reopen). PO Issued added for B2B business term payments.**
- **Q: What is the access control model for order permissions?** → **A: Role-Based Access Control (RBAC) with four roles: Customer (own orders only), Employee (assigned orders), Manager (department orders), Admin (all orders). Confidential orders require appropriate role authorization.**
- **Q: How should the system authenticate users?** → **A: Integrate with existing MALIEV Auth Service for single sign-on across all MALIEV microservices. JWT-based authentication.**
- **Q: What file formats and size limits should be supported for order uploads?** → **A: All common formats supported - CAD files (STL, STEP, IGES, OBJ, etc.), images (JPG, PNG, PDF), documents (PDF, DOC, DOCX), archives (ZIP, RAR). Max 100MB per file, 500MB total per order.**
- **Q: How should the system integrate with Shopify for order imports?** → **A: OUT OF SCOPE - Shopify product sales (injection molding machines, materials, tools) handled by separate E-commerce/Inventory Service. Order Service focuses exclusively on custom manufacturing/service orders created via Order Service API.**
- **Q: Should status update notes be visible to customers or internal only?** → **A: Separate notes - Internal notes (employee-only visibility) and Customer-facing notes (visible to customers). Employees can add both types when updating status.**
- **Q: Should non-NDA customers be able to request confidential treatment for specific orders?** → **A: No - only customers with signed NDAs can have confidential orders. Confidentiality is automatic for NDA customers, not available as manual option for non-NDA customers.**
- **Q: Where should customer NDA status and agreement details come from?** → **A: Separate Customer Service - NDA data managed in dedicated customer/CRM microservice. Order Service queries NDA status via API when creating orders.**
- **Q: What batch operations should employees be able to perform on orders?** → **A: Full CRUD batch operations - create multiple orders, update multiple orders, delete multiple orders, bulk assignment to employees/departments, bulk status changes.**
- **Q: What are the notification requirements for order status changes?** → **A: Real-time interface-based notifications - Order Service defines notification interface, implementation deferred for later. Subscription-based multi-channel delivery (LINE chat, email, etc.) for customers who opt-in.**
- **Q: How should payment status be tracked?** → **A: External payment service - Payment handled entirely by external payment/billing service. Order Service only references payment ID, does not track payment status internally.**
- **Q: What is the data retention period for orders and files?** → **A: Standard compliance - 7 years for order data (tax/audit compliance), 30 days post-completion for files, then automatic deletion.**
- **Q: What should happen when Customer Service API is unavailable during order creation?** → **A: Block creation - Reject order creation until API is available, return error to user. Ensures NDA status accuracy, no orders created with unknown confidentiality.**
- **Q: What should happen when a batch operation partially fails?** → **A: All-or-nothing rollback - If any operation in the batch fails, rollback all changes and return error with details of failed items. Ensures data consistency.**
- **Q: What concurrency control strategy should be used for order updates?** → **A: Optimistic locking - Use version numbers/timestamps to detect conflicts. Allow concurrent updates, detect conflicts on commit, return error if version mismatch.**
- **Q: What is the file upload retry mechanism?** → **A: Automatic retry - System automatically retries failed uploads up to 3 times with exponential backoff before reporting error to user.**
- **Q: What is the cancellation policy for orders after production has started?** → **A: Allow cancellation with partial charges - Orders can be cancelled even after In Progress. Full refund for work not completed, charge for materials and labor already used.**
- **Q: What are the Payment/Billing Service endpoints and what payment data should Order Service store?** → **A: Payment Service endpoints: GET https://api.maliev.com/payments/v1/{paymentId} (get payment status), POST https://api.maliev.com/payments/v1/payments (create payment record), POST https://api.maliev.com/payments/v1/refunds (process refunds/partial charges). Order Service stores only: payment status (paid/not paid) and paymentId reference for querying Payment Service.**
- **Q: What are the Notification Service endpoints and payload format?** → **A: Notification Service endpoints: POST https://api.maliev.com/notifications/v1/send (send notification, auto-handles multi-channel delivery including LINE, email), GET https://api.maliev.com/notifications/v1/{notificationId}/status (get notification status). Payload format not fully specified yet - implement starting interface for order update notifications only.**
- **Q: What are the MALIEV Auth Service endpoints and what is the best approach for RBAC with dual databases (Customer DB and Employee DB)?** → **A: Auth Service endpoint: POST https://api.maliev.com/auth/v1/validate (validate access token in request header). Recommended RBAC approach: Context-based authorization where Auth Service validates token and returns user context (userType: customer/employee, userId, roles). Order Service implements business logic based on user type. Customers create orders from their account page. Employees create and manage all orders.**
- **Q: What are the Employee Service endpoints?** → **A: Employee Service endpoints: GET https://api.maliev.com/employees/v1/{employeeId} (get employee details), GET https://api.maliev.com/employees/v1/departments/{departmentId} (list employees by department).**
- **Q: What are the File Upload Service endpoints and how should file storage be managed?** → **A: File Upload Service endpoints: POST https://api.maliev.com/uploads/v1/files (upload file to GCS bucket using FileUploadRequest model with IFormFile, ObjectPath like "orders/{orderId}/files/drawing.pdf", AccessLevel, ServiceMetadata), GET https://api.maliev.com/uploads/v1/files/path?objectPath={objectPath} (download file), DELETE https://api.maliev.com/uploads/v1/files/path?objectPath={objectPath} (delete file). Upload Service handles actual GCS bucket operations.**
- **Q: What are the Material Service endpoints and how should material/color/surface finishing selections be managed?** → **A: Material Service endpoints: GET /materials/v1/materials?serviceCategoryId={id} (list materials for service type), GET /materials/v1/materials/{materialId}/colors (available colors for material), GET /materials/v1/materials/{materialId}/surface-finishings?colorId={id} (available finishings for material/color). Order Service stores integer references (materialId, colorId, surfaceFinishingId) and validates via Material Service. Material Service enforces constraints (e.g., certain colors only available for specific materials). Order Service does NOT calculate costs - only stores quote amount from Quoting Service.**
- **Q: How should order notes be managed and what's the difference from status notes?** → **A: Two note systems: (1) Status-specific notes: tied to status changes (OrderStatus.internal_notes, OrderStatus.customer_notes), (2) General order notes: separate table (order_notes) with full history tracking - customer notes (visible to all) and internal notes (employees only). Employees can add internal notes for coordination ("VIP customer", "material delayed"). Customers can add notes about requirements. Each note records: noteType, noteText, createdBy, createdAt.**

---

## External Service Endpoints

The Order Service depends on several external MALIEV microservices. All endpoints follow the base URL pattern `https://api.maliev.com/{service}/v1/`.

### Customer Service API
**Base URL**: `https://api.maliev.com/customers`

- **Get Customer Details**
  - `GET /v1/{customerId}`
  - Retrieves customer information for order validation
  - Required for every order creation (orders must belong to a customer)

- **Get NDA Status**
  - `GET /v1/{customerId}/nda`
  - Retrieves customer's NDA agreement status
  - Determines automatic confidentiality for all customer orders
  - Order creation blocked if API unavailable (no unknown confidentiality)

### Payment Service API
**Base URL**: `https://api.maliev.com/payments`

- **Get Payment Status**
  - `GET /v1/{paymentId}`
  - Retrieves payment information when order references a payment

- **Create Payment Record**
  - `POST /v1/payments`
  - Creates payment record for new orders

- **Process Refund/Partial Charge**
  - `POST /v1/refunds`
  - Handles refunds for cancelled orders and partial charges for work completed

**Order Service Storage**: Order Service stores only payment status (paid/unpaid) and paymentId reference.

### Notification Service API
**Base URL**: `https://api.maliev.com/notifications`

- **Send Notification**
  - `POST /v1/send`
  - Sends order status update notifications
  - Service auto-handles multi-channel delivery (LINE chat, email, etc.)
  - Payload format: Basic interface for order update notifications (full specification deferred)

- **Get Notification Status**
  - `GET /v1/{notificationId}/status`
  - Tracks notification delivery status

### MALIEV Auth Service API
**Base URL**: `https://api.maliev.com/auth`

- **Validate Access Token**
  - `POST /v1/validate`
  - Validates JWT access token from request header
  - Returns user context: `{ userType: "customer" | "employee", userId: string, roles: string[] }`
  - Supports dual authentication sources (Customer DB and Employee DB)

**RBAC Architecture**: Auth Service abstracts dual-database complexity. Returns unified user context. Order Service implements business logic based on userType:
- **Customers**: Create orders from account page, access own orders only
- **Employees**: Create and manage all orders, access based on role (Employee/Manager/Admin)

### Employee Service API
**Base URL**: `https://api.maliev.com/employees`

- **Get Employee Details**
  - `GET /v1/{employeeId}`
  - Retrieves employee information for order assignment

- **List Employees by Department**
  - `GET /v1/departments/{departmentId}`
  - Retrieves all employees in a department for bulk assignment and Manager role access control

### File Upload Service API
**Base URL**: `https://api.maliev.com/uploads`

- **Upload File**
  - `POST /v1/files`
  - Request Model:
    ```csharp
    public class FileUploadRequest
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string ObjectPath { get; set; } // e.g., "orders/{orderId}/files/drawing.pdf"

        public StorageOptions? StorageOptions { get; set; }
        public Dictionary<string, string>? ServiceMetadata { get; set; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Internal;
    }
    ```
  - Service handles actual GCS bucket upload operations
  - Supports automatic retry (3 attempts, exponential backoff)

- **Download File**
  - `GET /v1/files/path?objectPath={objectPath}`
  - Retrieves file from GCS bucket by object path

- **Delete File**
  - `DELETE /v1/files/path?objectPath={objectPath}`
  - Removes file from GCS bucket (used for 30-day retention policy)

### Material Service API
**Base URL**: `https://api.maliev.com/materials`

- **List Materials for Service Type**
  - `GET /v1/materials?serviceCategoryId={id}&processTypeId={id}`
  - Returns available materials for specific service type
  - Example: 3D Printing FDM returns PLA, ABS, PETG; CNC Machining returns Aluminum, Steel, Brass

- **Get Material Details**
  - `GET /v1/materials/{materialId}`
  - Retrieves material information (name, properties, etc.)
  - Used for validation and cache population

- **Get Available Colors for Material**
  - `GET /v1/materials/{materialId}/colors`
  - Returns color options available for specific material
  - Example: PLA returns [Black, White, Red], Steel returns [Raw] only

- **Get Available Surface Finishings**
  - `GET /v1/materials/{materialId}/surface-finishings?colorId={colorId}`
  - Returns finishing options for material/color combination
  - Material Service enforces constraints (e.g., sanding only for 3D printing materials, heat treat only for steel)

**Order Service Storage**: Order Service stores integer references (materialId, colorId, surfaceFinishingId) and denormalized cache (materialName, colorName, surfaceFinishingName) for display performance. Validation via GET endpoints ensures selections are valid.

**Note**: Order Service does NOT call cost endpoints. Quoting Service retrieves material costs and calculates total quote. Order Service only stores final quoted amount.

---

## User Scenarios & Testing

### Primary User Story
A customer visits MALIEV's website to place an order for custom manufacturing services (e.g., 3D printing, CNC machining). They create an order specifying the service type, upload necessary design files, and optionally mark the order as confidential under NDA. Throughout the manufacturing process, the customer can track the order status in real-time. Meanwhile, MALIEV employees receive the order, assign it to appropriate technicians, update its progress through various stages (received, in-production, quality-check, completed, shipped), and communicate updates back to the customer.

### Acceptance Scenarios

1. **Given** a customer wants to order a 3D printing service, **When** they create a new order with service type "3D Printing - FDM" and upload STL files, **Then** the system must accept the order, generate a unique order ID, and set the initial status to "Received"

2. **Given** an employee needs to update an order's progress, **When** they change the status from "Received" to "In Production" and add notes, **Then** the system must persist the status change with timestamp and employee information

3. **Given** a customer has placed a confidential order, **When** they mark the order as NDA-protected, **Then** the system must restrict access to authorized personnel only and display confidentiality indicators

4. **Given** a customer wants to check their order progress, **When** they retrieve the order by order ID, **Then** the system must display current status, timestamps, uploaded files, and status history

5. **Given** an order requires multiple file uploads, **When** the customer or employee uploads design files, specifications, or reference images, **Then** the system must associate these files with the order and track file metadata

6. **Given** the business offers diverse service categories, **When** creating an order, **Then** the system must allow selection from predefined service categories (3D Printing, 3D Scanning, CNC Machining, etc.) with specific process types

7. **Given** a customer has a signed NDA agreement on file, **When** they create any new order, **Then** the system must automatically mark the order as confidential without requiring manual selection

8. **Given** an employee updates an order status, **When** they add both an internal note ("Waiting for material supplier") and a customer-facing note ("Order processing is on track"), **Then** the system must store both notes separately, showing only the customer-facing note to the customer while displaying both to employees

9. **Given** a manager needs to assign multiple orders to their team, **When** they select 20 orders and bulk assign them to 3 different employees, **Then** the system must update all order assignments in a single operation and record the assignment change for each order

10. **Given** a customer has subscribed to LINE chat and email notifications, **When** their order status changes from "In Progress" to "Finished", **Then** the system must trigger a notification event to be delivered via both LINE chat and email channels

11. **Given** an order is currently "In Progress" with 40% work completed, **When** a customer cancels the order, **Then** the system must calculate partial charges for materials and labor used, process refund for remaining 60%, and transition order to Cancelled terminal state

### Edge Cases

- What happens when a customer tries to access another customer's confidential order? System enforces RBAC - Customer role can only access own orders, request denied with 403 Forbidden.
- How does the system handle file upload failures or incomplete uploads? System validates file size (max 100MB per file, 500MB total per order) and format before upload. Failed uploads automatically retry up to 3 times with exponential backoff. If all retries fail, return error to user.
- What happens when an order is cancelled after production has started? System allows cancellation at any stage. Calculates partial charges for materials and labor already used. Full refund issued for work not completed. Transition to Cancelled terminal state.
- What happens when Customer Service API is unavailable during order creation? System rejects order creation and returns error to user (503 Service Unavailable). No orders created without NDA status verification to ensure confidentiality accuracy.
- What happens when Material Service API is unavailable during order creation? System rejects orders that require material validation. Orders without material selections (consulting, design services) can proceed.
- What happens when a batch operation partially fails (e.g., 8 of 10 orders updated successfully, 2 failed)? System uses all-or-nothing transaction strategy - rollback all changes if any operation fails, return error with details of failed items. Ensures data consistency.
- What happens when multiple employees try to update the same order simultaneously? System uses optimistic locking with version numbers. First update succeeds. Subsequent updates on stale version return 409 Conflict error, requiring user to refresh and retry.
- How long are order files retained after order completion? Files are automatically deleted 30 days after order reaches terminal state (Shipped, Cancelled, Rejected, Declined, Expired). Order metadata (including notes) retained for 7 years for compliance.
- What happens when a customer tries to view internal notes? System enforces access control - customers can only view customer-type notes, internal notes filtered out from API responses.
- How does the system handle orders without files? Files are optional at creation. Consulting services, design orders (starting with verbal requirements), and scanning orders (starting with photos) may not have CAD files initially.

## Requirements

### Functional Requirements

#### Order Management
- **FR-001**: System MUST allow customers to create orders specifying service type, process type, quantity, and requirements
- **FR-002**: System MUST generate unique order identifiers for each order
- **FR-003**: System MUST support multiple service categories including: 3D Printing (FDM, DLP, SLA, DMLS, SLS, MJF), 3D Scanning (Structured Light, Laser, on-site, in-house), 3D Design, Reverse Engineering, CNC Machining (mill, turn, 4-5 axis), Silicone Mold Making, Casting, Injection Molding, Surface Finishing, Wire Cut, EDM
- **FR-004**: System MUST automatically mark ALL orders as confidential when the customer has a signed NDA agreement
- **FR-005**: System MUST NOT allow manual confidential marking for customers without signed NDAs (confidentiality requires NDA agreement)
- **FR-006**: System MUST support material/color/surface finishing selection via Material Service integration:
  - Store integer references (materialId, colorId, surfaceFinishingId)
  - Validate selections via Material Service GET endpoints
  - Cache display names (materialName, colorName, surfaceFinishingName) for performance
- **FR-007**: System MUST support batch operations for employees including:
  - Bulk order creation (multiple orders in single request)
  - Bulk order updates (modify multiple orders simultaneously)
  - Bulk order deletion (delete multiple orders simultaneously)
  - Bulk assignment (assign multiple orders to employees/departments)
  - Bulk status changes (update status for multiple orders)
- **FR-008**: System MUST use all-or-nothing transaction strategy for batch operations (rollback all changes if any operation fails, return error with failed item details)
- **FR-009**: System MUST implement optimistic locking for order updates using version numbers or timestamps to detect concurrent modification conflicts
- **FR-010**: System MUST return conflict error (409 Conflict) when an update is attempted on a stale order version
- **FR-011**: System MUST allow order cancellation at any status (including In Progress and later stages)
- **FR-012**: System MUST calculate partial charges for cancelled orders based on materials consumed and labor completed
- **FR-013**: System MUST support refund processing for work not completed on cancelled orders (refund amount calculation delegated to external payment service)

#### Quantity & Date Tracking
- **FR-014**: System MUST track quantity information for manufacturing orders:
  - ordered_quantity: Total parts/units requested by customer
  - manufactured_quantity: Running total of completed units (increments as batches finish)
  - remaining_quantity: Computed field (ordered_quantity - manufactured_quantity)
- **FR-015**: System MUST track date information for manufacturing orders:
  - lead_time_days: Expected number of days to complete order
  - promised_delivery_date: Date committed to customer
  - actual_delivery_date: Date order actually shipped/delivered
- **FR-016**: System MUST allow quantity and date fields to be nullable (not all order types require quantity tracking - consulting, design services may not need these fields)

#### Service-Specific Attributes
- **FR-017**: System MUST store service-specific attributes in dedicated tables per service type for queryability:
  - **3D Printing**: thread_tap_required, insert_required, part_marking, part_assembly_test_required
  - **3D Scanning**: required_accuracy (e.g., "±0.1mm"), scan_location (in-house or address), output_file_formats (CSV: "STL,STEP,PLY"), deviation_report_requested
  - **CNC Machining**: tap_required, tolerance (e.g., "ISO 2768-1 ±0.125mm"), surface_roughness (e.g., "3.2µm Ra"), inspection_type (e.g., "cmm-formal-report")
  - **Sheet Metal**: thickness (e.g., "1.5mm"), welding_required, welding_details, tolerance, inspection_type
  - **3D Design**: complexity_level (Simple/Medium/Complex), deliverables (CSV list), design_software, revision_rounds
- **FR-018**: System MUST support standard SQL queries on service attributes (e.g., "all CNC orders with tolerance ±0.05mm")

#### File Management
- **FR-019**: System MUST allow multiple file uploads per order (design files, specifications, references)
- **FR-020**: System MUST NOT require files at order creation (consulting, design services may start without files)
- **FR-021**: System MUST store file metadata including filename, upload timestamp, file size, uploader identity
- **FR-022**: System MUST associate files with specific orders
- **FR-023**: System MUST classify files with role system:
  - **Input**: Files provided by customer (CAD files, sketches, reference images, drawings)
  - **Output**: Files delivered by MALIEV (finished CAD models, scan data, reports)
  - **Supporting**: Other documentation (quotes, specifications, process notes)
- **FR-024**: System MUST classify files by category (CAD, Drawing, Image, Document, Archive, Other)
- **FR-025**: System MUST track design_units for CAD files (mm, inch, cm, m) - nullable field since one order may have files in different units
- **FR-026**: System MUST support all common file formats including:
  - CAD files: STL, STEP, IGES, OBJ, and other common 3D formats
  - Images: JPG, PNG, PDF
  - Documents: PDF, DOC, DOCX
  - Archives: 7Z, ZIP, RAR
- **FR-027**: System MUST enforce file size limits: maximum 100MB per individual file, maximum 500MB total per order
- **FR-028**: System MUST automatically retry failed file uploads up to 3 times with exponential backoff before returning error to user
- **FR-029**: System MUST allow filtering files by role (e.g., show only Input files for manufacturing, only Output files for customer downloads)

#### Status Tracking
- **FR-030**: System MUST track order status with the following workflow:
  - **Primary Flow**: New → Reviewing → [Rejected OR Reviewed] → Quoted → [Declined OR Accepted OR Expired] → [Paid OR PO Issued] → In Progress → Finished → Shipped
  - **Exception Flows**: In Progress ↔ On Hold (pause/resume), Finished/Shipped → Reopen → In Progress (rework), Paid → Cancelled, Any status → Cancelled (with partial charge calculation)
  - **Terminal States**: Rejected, Declined, Expired, Cancelled, Shipped (unless Reopen triggered)
  - **Valid Transitions**: System MUST enforce state transition rules (e.g., Reviewing can only go to Rejected or Reviewed, not directly to Shipped)
- **FR-031**: System MUST support PO Issued status for B2B orders with business payment terms (alternative to Paid for customers with purchase orders)
- **FR-032**: System MUST record timestamp for each status change
- **FR-033**: System MUST record which employee updated each status
- **FR-034**: System MUST allow customers to retrieve current order status
- **FR-035**: System MUST maintain complete status history for each order
- **FR-036**: Employees MUST be able to add two types of notes when updating order status:
  - **Internal Notes**: Visible only to employees (Employee, Manager, Admin roles)
  - **Customer-Facing Notes**: Visible to both customers and employees
- **FR-037**: System MUST display only customer-facing notes to customers, while employees can view both internal and customer-facing notes

#### Order Notes Management
- **FR-038**: System MUST maintain separate order notes system independent of status changes
- **FR-039**: System MUST support two note types:
  - **customer**: Notes visible to all users (customers and employees) - for customer requirements, preferences, special requests
  - **internal**: Notes visible only to employees (Employee, Manager, Admin roles) - for internal coordination ("VIP customer", "material delayed", "rush order")
- **FR-040**: System MUST track full note history with metadata:
  - noteId (unique identifier)
  - noteType (customer or internal)
  - noteText (content)
  - createdBy (user who created note)
  - createdAt (timestamp)
- **FR-041**: System MUST allow customers to create only customer-type notes
- **FR-042**: System MUST allow employees to create both customer-type and internal-type notes
- **FR-043**: System MUST prevent customers from viewing internal notes
- **FR-044**: System MUST display notes in chronological order (newest first or oldest first)

#### Access Control & Security
- **FR-045**: System MUST implement Role-Based Access Control (RBAC) with the following roles:
  - **Customer Role**: Access own orders only
  - **Employee Role**: Access orders assigned to them
  - **Manager Role**: Access all orders within their department
  - **Admin Role**: Access all orders across the system
- **FR-046**: System MUST restrict confidential orders to authorized roles only (Employee/Manager/Admin with appropriate assignment or department access)
- **FR-047**: System MUST authenticate users via integration with existing MALIEV Auth Service using JWT-based authentication (single sign-on across all MALIEV microservices)
- **FR-048**: System MUST support department-based order assignment for Manager role access control
- **FR-049**: System MUST log all access attempts to confidential orders for audit purposes

#### Integration & Data
- **FR-050**: System MUST integrate with Customer Service API to query customer NDA status when creating orders
- **FR-051**: System MUST automatically mark orders as confidential based on NDA status retrieved from Customer Service
- **FR-052**: System MUST reject order creation and return error when Customer Service API is unavailable (no order creation without NDA status verification)
- **FR-053**: System MUST integrate with Material Service API to validate material/color/surface finishing selections
- **FR-054**: System MUST cache material display names (materialName, colorName, surfaceFinishingName) with 24-hour TTL for performance
- **FR-055**: System MUST reference external payment ID from payment/billing service (payment status tracking delegated to external payment service)
- **FR-056**: System MUST retain order data for 7 years for tax and audit compliance requirements
- **FR-057**: System MUST automatically delete order files 30 days after order completion (terminal states: Shipped, Cancelled, Rejected, Declined, Expired)
- **FR-058**: System MUST preserve order metadata (status history, timestamps, assignments, notes) for the full 7-year retention period even after files are deleted

#### Notifications & Communication
- **FR-059**: System MUST define notification interface for real-time order status change events
- **FR-060**: System MUST trigger notification events when order status changes (implementation deferred to external notification service)
- **FR-061**: System MUST support subscription-based notification delivery to customers who opt-in
- **FR-062**: System MUST support multi-channel notification delivery including LINE chat, email, and other channels (implementation delegated to notification service)
- **FR-063**: System MUST track customer notification preferences and subscription status

### Key Entities

- **Order**: Represents a customer request for manufacturing or prototyping services. Attributes include:
  - **Identity**: unique order ID, customer identifier
  - **Service Details**: service category, process type (nullable - consulting/procurement may not have process)
  - **Materials** (nullable): materialId, colorId, surfaceFinishingId (integer references to Material Service), cached display names (materialName, colorName, surfaceFinishingName)
  - **Quantity Tracking** (nullable - manufacturing orders only): ordered_quantity, manufactured_quantity, remaining_quantity (computed)
  - **Date Tracking** (nullable - manufacturing orders only): lead_time_days, promised_delivery_date, actual_delivery_date
  - **Quote**: quoted_amount, quote_currency
  - **Generic**: requirements (text field for additional specifications)
  - **Audit**: creation date, current status, confidentiality flag, external payment reference ID, version number/timestamp (optimistic locking)

- **Service Category**: Represents the type of service offered (3D Printing, CNC Machining, 3D Scanning, 3D Design, Sheet Metal, Consulting, etc.). Each category contains specific process types (e.g., 3D Printing includes FDM, SLA, MJF).

- **Process Type**: Specific manufacturing process within a service category (e.g., FDM, DLP for 3D Printing; 4-axis, 5-axis for CNC Machining). Nullable for orders that don't fit standard processes (consulting, procurement).

- **Order 3D Printing Attributes**: Service-specific attributes for 3D printing orders. One-to-one relationship with Order. Attributes: thread_tap_required (boolean), insert_required (boolean), part_marking (string), part_assembly_test_required (boolean).

- **Order CNC Machining Attributes**: Service-specific attributes for CNC machining orders. Attributes: tap_required (boolean), tolerance (string, e.g., "ISO 2768-1 ±0.125mm"), surface_roughness (string, e.g., "3.2µm Ra"), inspection_type (string, e.g., "cmm-formal-report").

- **Order Sheet Metal Attributes**: Service-specific attributes for sheet metal orders. Attributes: thickness (string, e.g., "1.5mm"), welding_required (boolean), welding_details (text), tolerance (string), inspection_type (string).

- **Order 3D Scanning Attributes**: Service-specific attributes for 3D scanning orders. Attributes: required_accuracy (string, e.g., "±0.1mm"), scan_location (text, null = in-house or customer address), output_file_formats (string, CSV list like "STL,STEP,PLY"), deviation_report_requested (boolean).

- **Order 3D Design Attributes**: Service-specific attributes for 3D design orders. Attributes: complexity_level (string: Simple/Medium/Complex), deliverables (string, CSV list), design_software (string, e.g., "SolidWorks"), revision_rounds (integer, default 2).

- **Order Status**: Represents the current state of an order in the fulfillment workflow. Status types include: New, Reviewing, Rejected, Reviewed, Quoted, Declined, Accepted, Expired, Paid, PO Issued, In Progress, On Hold, Finished, Shipped, Reopen, Cancelled. Contains status type, timestamp, employee who updated it, optional internal notes (employee-only), optional customer-facing notes (visible to customers and employees), and enforces valid state transitions per workflow rules.

- **Order File**: Represents files uploaded to an order. Attributes include:
  - **Identity**: file ID, order ID reference
  - **Classification**: file_role (Input/Output/Supporting), file_category (CAD/Drawing/Image/Document/Archive/Other)
  - **CAD-Specific**: design_units (mm/inch/cm/m, nullable - for CAD files only, one order can have files in different units)
  - **Metadata**: file reference/path, filename, upload timestamp, file size (max 100MB per file), uploader identity
  - **Constraints**: Total size per order limited to 500MB
  - **Formats**: CAD (STL, STEP, IGES, OBJ), images (JPG, PNG, PDF), documents (PDF, DOC, DOCX), archives (ZIP, RAR)

- **Order Note**: Represents general order notes separate from status-specific notes. Attributes include:
  - **Identity**: noteId (unique), orderId (reference)
  - **Content**: noteType (customer/internal), noteText
  - **Audit**: createdBy (user identifier), createdAt (timestamp)
  - **Access Control**: customer notes visible to all, internal notes visible only to employees

- **Customer**: Entity representing the person or business placing the order. NDA agreement status and details are retrieved from Customer Service API when creating orders. All orders from customers with signed NDAs are automatically marked confidential. Customers without NDAs cannot request confidential treatment. Includes notification subscription preferences for multi-channel delivery (LINE chat, email, etc.).

- **Notification Subscription**: Tracks customer opt-in preferences for receiving order status notifications via various channels (LINE chat, email, SMS, etc.).

- **Employee**: Entity representing MALIEV staff who process orders. Includes role (Employee, Manager, Admin), department assignment, and order assignment information. Role determines access scope: Employee (assigned orders), Manager (department orders), Admin (all orders).

- **Confidentiality Settings**: Tracks NDA status and authorized personnel for confidential orders.

---

## Review & Acceptance Checklist

### Content Quality
- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness
- [x] No [NEEDS CLARIFICATION] markers remain - **All 22 clarifications resolved**
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

---

## Execution Status

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked (22 clarifications identified)
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [x] External service endpoints documented
- [x] All clarifications resolved
- [x] Review checklist passed

---

## RBAC Architecture Recommendation

### Context-Based Authorization Pattern

Based on the dual-database architecture (Customer DB and Employee DB), the recommended approach is **Context-Based Authorization**:

**Architecture**:
1. **Single Validation Flow**: Order Service validates all requests through single Auth Service endpoint
2. **User Context Response**: Auth Service returns unified user context:
   ```json
   {
     "userType": "customer" | "employee",
     "userId": "string",
     "roles": ["string"]
   }
   ```
3. **Business Logic in Order Service**: Order Service implements authorization logic based on userType

**Benefits**:
- Auth Service abstracts dual-database complexity
- Order Service receives clear, consistent user context
- Follows microservice separation of concerns
- Single integration point reduces coupling
- Supports future user type additions

**User Flows**:
- **Customers**: Authenticate via Customer DB → Create orders from account page → Access own orders only
- **Employees**: Authenticate via Employee DB → Create and manage all orders → Access based on role:
  - Employee role: Assigned orders
  - Manager role: Department orders
  - Admin role: All orders

---
