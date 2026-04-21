# Task: Customer Claim / RMA Module

## Objective
Implement Customer Claim (RMA) module for cases where a customer returns a sold serial-controlled item for warranty claim or service.

## Recommended Work Split

This module should be implemented in phases because it touches serial status, customer warranty, supplier claim, stock movement, and after-sales workflow.

### Phase 1: Customer Claim Intake
Create the basic customer claim document and validation.

### Phase 2: Claim Resolution Workflow
Add action buttons and status transitions for receiving, repairing/rejecting, returning to customer, or closing.

### Phase 3: Replacement / Supplier Claim Link
Support replacement serials and optional link to supplier claim.

Do not implement all phases in one risky change unless explicitly requested.

---

## Phase 1 Requirements: Customer Claim Intake

1. Create CustomerClaimHeaders table
2. Create CustomerClaimDetails table
3. Create Customer Claim list and details page
4. Create Customer Claim from selected serial
   - from Serial Inquiry
   - optionally from Invoice Details serial line
5. Validate selected serial:
   - Serial must exist
   - Serial must have `CurrentCustomerId`
   - Serial must have `InvoiceId`
   - Serial status must be `Sold`
   - Customer warranty must not be expired
   - Do not allow duplicate open customer claim for same serial
6. On create:
   - create claim header/detail
   - set claim status = `Open`
   - set serial status = `CustomerClaim`
7. Show:
   - customer info
   - item info
   - serial info
   - invoice info
   - customer warranty start/end
   - problem description
   - claim status
   - document history
8. Add search/filter/paging to Customer Claim list
9. Add Customer Claims menu under after-sales/service or inventory module

## Phase 1 Constraints
- Do not change invoice balances
- Do not change payment/receipt logic
- Do not create replacement serial yet
- Do not create supplier claim automatically
- Do not move stock quantity yet

---

## Phase 2 Requirements: Resolution Workflow

1. Add workflow action buttons:
   - Receive Item
   - Send To Supplier
   - Repair Complete
   - Reject Claim
   - Return To Customer
   - Close Claim
   - Cancel Claim
2. If action is disabled:
   - disable button
   - show clear reason/message
3. Status rules:
   - Open
   - Received
   - SentToSupplier
   - Repairing
   - ReadyToReturn
   - ReturnedToCustomer
   - Rejected
   - Closed
   - Cancelled
4. Keep cancelled and closed claims visible in history
5. Update serial status consistently:
   - Open/Received/Repairing -> CustomerClaim
   - SentToSupplier -> ClaimedToSupplier
   - ReturnedToCustomer/Closed -> Sold
   - Rejected -> Sold
   - Cancelled -> Sold, if original serial was sold before claim
6. Use database transaction for any action that updates both claim and serial

## Phase 2 Constraints
- Do not auto-create supplier claim unless Phase 3 is approved
- Do not auto-replace stock

---

## Phase 3 Requirements: Replacement / Supplier Claim Link

1. Allow claim to link to a Supplier Claim / SerialClaimLog
2. Allow replacement serial selection when customer receives replacement item
3. Validate replacement serial:
   - must be `InStock`
   - must be same ItemId or compatible item rule
   - must not already be sold/claimed
4. On replacement:
   - original serial status = `Replaced` or `Defective`
   - replacement serial status = `Sold`
   - replacement serial CurrentCustomerId = claim customer
   - replacement serial InvoiceId may reference original invoice or claim reference, depending on design
5. Show replacement history in claim details

## Phase 3 Constraints
- Replacement accounting impact is out of scope unless explicitly requested
- Do not change original invoice amount automatically

---

## Print / Document

Print is optional for Phase 1.

If implemented later, create A4 Customer Claim form showing:
- company info
- claim info
- customer info
- item/serial info
- invoice reference
- warranty info
- problem description
- result / resolution
- customer signature
- service staff signature

---

## Audit Requirements

All claim workflow actions must record:
- CreatedByUserId / CreatedDate
- UpdatedByUserId / UpdatedDate
- ReceivedByUserId / ReceivedDate
- SentToSupplierByUserId / SentToSupplierDate
- ResolvedByUserId / ResolvedDate
- ReturnedByUserId / ReturnedDate
- ClosedByUserId / ClosedDate
- CancelledByUserId / CancelledDate

Only add action-specific fields when the action is implemented.

---

## Deliverables

### Phase 1 Deliverables
1. SQL script
2. Entity models
3. DbContext mapping
4. Controller
5. List page
6. Create page from serial
7. Details page
8. Serial Inquiry action button
9. Search/filter/paging
10. Build verification

### Phase 2 Deliverables
1. Workflow actions
2. Status validation
3. Serial status update rules
4. Clear disabled action reasons
5. Audit history
6. Build verification

### Phase 3 Deliverables
1. Supplier claim link
2. Replacement serial workflow
3. Replacement history
4. Build verification
