# Invoice Status and Cancel Specification

## Objective
Control invoice status, link invoice to quotation, and implement safe invoice cancellation.

---

## 1. Invoice Header Requirements

### Update: InvoiceHeaders
Required fields:
- InvoiceId (int, PK)
- InvoiceNo (string, unique)
- InvoiceDate (datetime)
- CustomerId (int, FK)
- SalespersonId (int, nullable, FK)
- QuotationId (int, nullable, FK to QuotationHeaders)
- ReferenceNo (string, nullable)
- Status (string: Draft / Issued / Cancelled)
- Subtotal (decimal(18,2))
- DiscountAmount (decimal(18,2))
- VatType (string)
- VatAmount (decimal(18,2))
- TotalAmount (decimal(18,2))
- PaidAmount (decimal(18,2))
- BalanceAmount (decimal(18,2))
- Remark (string, nullable)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## 2. Quotation Link Rules

1. If invoice is created from quotation:
   - InvoiceHeaders.QuotationId must be set
2. If invoice is created directly:
   - QuotationId = null
3. Invoice screen and print should show related QuotationNo if QuotationId exists

---

## 3. Invoice Status Rules

### Draft
- Invoice can be edited
- User can:
  - add/remove lines
  - change qty
  - change price
  - change selected serials
  - change customer warranty
- No final posting effect should be locked yet

### Issued
- Invoice is finalized
- Stock deduction and serial assignment already applied
- Invoice must become read-only
- Do not allow direct edit

### Cancelled
- Invoice is inactive
- Must remain visible for audit/history
- Must be read-only

---

## 4. Edit Rules

1. Only Draft invoice can be edited
2. Issued invoice cannot be edited directly
3. Cancelled invoice cannot be edited
4. If user made mistake after issue:
   - cancel invoice
   - create new invoice

---

## 5. Cancel Invoice Rules

### Preconditions
1. Allow cancel only if invoice Status = Issued or Draft
2. If invoice has related payment allocation or PaidAmount > 0:
   - do not allow cancel
   - show message:
     "Cannot cancel invoice with payment. Cancel payment first."

### If cancelling Draft
- Set Status = Cancelled
- No stock reversal needed
- No serial reversal needed

### If cancelling Issued
Must reverse all sales effects safely:

#### Stock
- For each invoice detail where Item.TrackStock = true:
  - add Qty back to Items.CurrentStock

#### Serial
- For each linked InvoiceSerial:
  - update related SerialNumbers:
    - Status = InStock
    - CurrentCustomerId = null
    - InvoiceId = null
    - CustomerWarrantyStartDate = null
    - CustomerWarrantyEndDate = null

#### Invoice
- Set InvoiceHeaders.Status = Cancelled

### Transaction
- Invoice cancel must run inside one database transaction
- If any step fails, rollback all

---

## 6. Quotation Status Interaction

If invoice was created from quotation:
- Keep quotation link for history
- Do not automatically reopen quotation for now
- Quotation remains Converted

---

## 7. Required Screens / Actions

### Invoice List
- Show Status
- Add Cancel action only when allowed

### Invoice View
- Show:
  - Status
  - Related QuotationNo (if any)
  - ReferenceNo
- If Draft:
  - allow Edit
- If Issued:
  - allow Cancel if no payment
- If Cancelled:
  - read-only

---

## 8. Validation Rules

1. Draft invoice only can be edited
2. Issued invoice cannot be edited
3. Cancelled invoice cannot be edited
4. Cannot cancel invoice if PaidAmount > 0
5. Cannot cancel invoice if payment allocation exists
6. Cancel issued invoice must restore stock and serial correctly

---

## 9. Deliverables for Codex

1. Update Invoice status rules
2. Lock invoice edit by status
3. Implement Cancel Invoice action
4. Reverse stock on issued invoice cancellation
5. Reverse serial assignment on issued invoice cancellation
6. Show related quotation reference in invoice screen
7. Use transaction for cancel logic