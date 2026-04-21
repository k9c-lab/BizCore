# Customer Claim / RMA Implementation Specification

## Objective
Implement Customer Claim / RMA for customer warranty claims against sold serial-controlled items.

The first implementation should focus on claim intake and traceability. Resolution, replacement, and supplier-claim linkage should be implemented as later phases.

---

## 1. Recommended Implementation Phases

### Phase 1: Intake
Create the claim document and mark the serial as under customer claim.

### Phase 2: Workflow
Add controlled status changes and serial status transitions.

### Phase 3: Replacement / Supplier Link
Support replacement serial and optional supplier claim relationship.

---

## 2. Tables

### Table: CustomerClaimHeaders
- CustomerClaimId (int, PK)
- CustomerClaimNo (nvarchar(30), unique)
- CustomerClaimDate (datetime2)
- CustomerId (int, FK Customers)
- InvoiceId (int, FK InvoiceHeaders, nullable)
- Status (nvarchar(30))
- ProblemDescription (nvarchar(1000), nullable)
- ResolutionRemark (nvarchar(1000), nullable)
- CancelReason (nvarchar(500), nullable)
- CreatedByUserId (int, FK Users, nullable)
- UpdatedByUserId (int, FK Users, nullable)
- ReceivedByUserId (int, FK Users, nullable)
- SentToSupplierByUserId (int, FK Users, nullable)
- ResolvedByUserId (int, FK Users, nullable)
- ReturnedByUserId (int, FK Users, nullable)
- ClosedByUserId (int, FK Users, nullable)
- CancelledByUserId (int, FK Users, nullable)
- CreatedDate (datetime2)
- UpdatedDate (datetime2, nullable)
- ReceivedDate (datetime2, nullable)
- SentToSupplierDate (datetime2, nullable)
- ResolvedDate (datetime2, nullable)
- ReturnedDate (datetime2, nullable)
- ClosedDate (datetime2, nullable)
- CancelledDate (datetime2, nullable)

### Table: CustomerClaimDetails
- CustomerClaimDetailId (int, PK)
- CustomerClaimId (int, FK CustomerClaimHeaders)
- SerialId (int, FK SerialNumbers)
- ItemId (int, FK Items)
- OriginalInvoiceId (int, FK InvoiceHeaders, nullable)
- ReplacementSerialId (int, FK SerialNumbers, nullable)
- LineRemark (nvarchar(500), nullable)

Phase 1 can support only one detail row per claim, but the table should allow future multi-line claims if needed.

---

## 3. Source Data

Use existing:
- SerialNumbers
- Items
- Customers
- InvoiceHeaders
- InvoiceDetails / InvoiceSerials when needed for invoice tracing
- SerialClaimLogs in Phase 3 only

SerialNumbers should already contain:
- SerialId
- SerialNo
- ItemId
- Status
- CurrentCustomerId
- InvoiceId
- CustomerWarrantyStartDate
- CustomerWarrantyEndDate
- SupplierId
- SupplierWarrantyStartDate
- SupplierWarrantyEndDate

---

## 4. Running Number

CustomerClaimNo format:
- CC-YYYYMM-XXXX

Example:
- CC-202604-0001

---

## 5. Status Values

### Phase 1
- Open
- Cancelled

### Phase 2
- Open
- Received
- SentToSupplier
- Repairing
- ReadyToReturn
- ReturnedToCustomer
- Rejected
- Closed
- Cancelled

### Phase 3
May add:
- Replaced

---

## 6. Serial Status Rules

Existing serial statuses include:
- InStock
- Sold
- ClaimedToSupplier

Add/standardize:
- CustomerClaim
- Replaced
- Defective

### Phase 1
On claim create:
- SerialNumbers.Status = CustomerClaim

On claim cancel:
- SerialNumbers.Status = Sold

### Phase 2
- Open / Received / Repairing / ReadyToReturn -> CustomerClaim
- SentToSupplier -> ClaimedToSupplier
- ReturnedToCustomer / Closed -> Sold
- Rejected -> Sold
- Cancelled -> Sold

### Phase 3 Replacement
- Original serial -> Replaced or Defective
- Replacement serial -> Sold

---

## 7. Validation Rules

### Create Claim
1. Serial must exist
2. Serial must be sold:
   - SerialNumbers.Status = Sold
3. Serial must have CurrentCustomerId
4. Serial must have InvoiceId
5. Customer warranty must exist:
   - CustomerWarrantyStartDate is not null
   - CustomerWarrantyEndDate is not null
6. Claim date must be within customer warranty period
7. Do not allow duplicate open customer claim for same SerialId
8. ItemId in claim detail must match serial.ItemId
9. CustomerId in claim header must match serial.CurrentCustomerId
10. InvoiceId in claim header/detail must match serial.InvoiceId

### Duplicate Open Claim
Treat these statuses as open:
- Open
- Received
- SentToSupplier
- Repairing
- ReadyToReturn
- ReturnedToCustomer

Do not block if prior claim is:
- Closed
- Cancelled
- Rejected

---

## 8. UI Requirements

### Customer Claim List
Show:
- Claim No.
- Claim Date
- Customer
- Serial No.
- Item
- Invoice No.
- Status
- Actions

Include:
- Search
- Status filter
- Date range filter
- Paging

Search by:
- Claim No.
- Customer code/name/tax ID
- Serial No.
- Item code/name/part number
- Invoice No.

### Create Page
Open from selected serial.

Show read-only:
- Serial No.
- Item Code
- Item Name
- Part Number
- Customer
- Invoice No.
- Customer warranty start/end
- Current serial status

Input:
- Claim Date
- Problem Description
- Remark

### Details Page
Show:
- Header information
- Customer information
- Serial/item/invoice information
- Warranty information
- Problem description
- Resolution remark
- Document history
- Action buttons based on status

### Serial Inquiry Integration
For each serial card:
- Show `Customer Claim` action when:
  - Status = Sold
  - CurrentCustomerId exists
  - InvoiceId exists
  - Customer warranty is active
  - no duplicate open customer claim exists
- If disabled, show clear reason/message

---

## 9. Workflow Actions

### Phase 1
- Create Claim
- Cancel Claim

### Phase 2
- Receive Item
- Send To Supplier
- Mark Repair Complete
- Reject Claim
- Return To Customer
- Close Claim

All actions must:
- validate current status
- update status
- update serial status when needed
- update audit fields
- run in a transaction if claim and serial are both updated

---

## 10. Audit

### Phase 1
Required:
- CreatedByUserId / CreatedDate
- UpdatedByUserId / UpdatedDate
- CancelledByUserId / CancelledDate

### Phase 2
Add:
- ReceivedByUserId / ReceivedDate
- SentToSupplierByUserId / SentToSupplierDate
- ResolvedByUserId / ResolvedDate
- ReturnedByUserId / ReturnedDate
- ClosedByUserId / ClosedDate

---

## 11. Business Logic Constraints

1. Do not change invoice totals
2. Do not change invoice payment status
3. Do not change receipt/payment logic
4. Do not auto-create supplier claim in Phase 1
5. Do not auto-replace serial in Phase 1
6. Do not adjust stock quantity in Phase 1
7. Keep claim history visible even after cancel/close

---

## 12. Suggested SQL Script Order

Next script:
- 022_customer_claim_module.sql

If Phase 2 adds fields later:
- 023_customer_claim_workflow_audit.sql

If Phase 3 adds replacement/supplier link:
- 024_customer_claim_replacement_supplier_link.sql

---

## 13. Phase 1 Deliverables

1. Add SQL script 022
2. Add entities:
   - CustomerClaimHeader
   - CustomerClaimDetail
3. Add DbContext mappings
4. Add controller:
   - CustomerClaimsController
5. Add views:
   - Index
   - Create
   - Details
6. Add Customer Claim action from Serial Inquiry / Stock Inquiry serial list
7. Add sidebar menu item
8. Implement validation
9. Implement serial status update to CustomerClaim on create
10. Implement cancel action returning serial status to Sold
11. Add search/filter/paging
12. Build verification

---

## 14. Phase 2 Deliverables

1. Add workflow action buttons
2. Add workflow validation and disabled reasons
3. Add status transitions
4. Add audit fields/actions
5. Build verification

---

## 15. Phase 3 Deliverables

1. Add supplier claim link support
2. Add replacement serial selection
3. Add replacement serial status updates
4. Add replacement history display
5. Build verification
