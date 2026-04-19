# Invoice Module Specification

## Objective
Implement direct sales invoice with serial selection, stock deduction, customer assignment, and customer warranty update.

---

## 1. Invoice Header

### Table: InvoiceHeaders
- InvoiceId (int, PK)
- InvoiceNo (string, unique)
- InvoiceDate (datetime)
- CustomerId (int, FK)
- SalespersonId (int, nullable, FK)
- Remark (string, nullable)
- Subtotal (decimal(18,2))
- DiscountAmount (decimal(18,2))
- VatType (string: VAT / NoVAT)
- VatAmount (decimal(18,2))
- TotalAmount (decimal(18,2))
- PaidAmount (decimal(18,2))
- BalanceAmount (decimal(18,2))
- Status (string: Draft / Issued / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## 2. Invoice Detail

### Table: InvoiceDetails
- InvoiceDetailId (int, PK)
- InvoiceId (int, FK)
- ItemId (int, FK)
- Qty (decimal(18,2))
- UnitPrice (decimal(18,2))
- DiscountAmount (decimal(18,2))
- LineTotal (decimal(18,2))
- Remark (string, nullable)

---

## 3. Invoice Serial

Use this table only for serial-controlled items.

### Table: InvoiceSerials
- InvoiceSerialId (int, PK)
- InvoiceDetailId (int, FK)
- SerialId (int, FK)

---

## 4. Source Data

Use existing tables:
- Customers
- Items
- Salespersons
- SerialNumbers

SerialNumbers already contain:
- SerialId
- ItemId
- SerialNo
- Status
- CurrentCustomerId
- InvoiceId
- SupplierId
- SupplierWarrantyStartDate
- SupplierWarrantyEndDate
- CustomerWarrantyStartDate
- CustomerWarrantyEndDate

---

## 5. Sales Rules

1. Invoice can be created directly without quotation
2. For non-serial-controlled item:
   - user enters Qty normally
3. For serial-controlled item:
   - user must select serial numbers
   - selected serial count must equal Qty
4. Only serials with Status = InStock can be selected
5. Serial must belong to the same ItemId
6. Same serial cannot be sold twice
7. On invoice issue:
   - reduce Items.CurrentStock by Qty if TrackStock = true
   - update selected SerialNumbers:
     - Status = Sold
     - CurrentCustomerId = InvoiceHeader.CustomerId
     - InvoiceId = current InvoiceId
8. Do not allow editing issued invoice in a way that breaks stock/serial consistency
9. Cancelled invoice must not be used as active sales document

---

## 6. Customer Warranty

Customer warranty is set during sales.

For serial-controlled items:
- allow input:
  - CustomerWarrantyStartDate
  - CustomerWarrantyEndDate

Rules:
1. Warranty dates can be entered per invoice detail line
2. All selected serials under that invoice detail line can share the same customer warranty dates
3. On invoice issue, update selected serials:
   - CustomerWarrantyStartDate
   - CustomerWarrantyEndDate

---

## 7. VAT Rules

1. User selects:
   - VAT
   - NoVAT
2. System calculates:
   - Subtotal
   - VatAmount
   - TotalAmount
3. User must not manually input VatAmount

---

## 8. Required Screens

### Invoice List
- InvoiceNo
- InvoiceDate
- Customer
- TotalAmount
- Status

### Invoice Create
- Select Customer
- Select Salesperson
- Select VatType
- Add item lines
- For serial-controlled item:
  - select serial numbers from available InStock serials
  - input customer warranty start/end

### Invoice View
- Show header
- Show detail
- Show assigned serials

---

## 9. Validation Rules

- Must select Customer
- Must have at least 1 detail line
- Qty > 0
- UnitPrice >= 0
- For serial-controlled item:
  - selected serial count must equal Qty
  - all serials must be InStock
  - all serials must belong to same item
- CustomerWarrantyEndDate must be >= CustomerWarrantyStartDate
- Do not allow stock to go below zero

---

## 10. Deliverables for Codex

1. Create InvoiceHeaders table
2. Create InvoiceDetails table
3. Create InvoiceSerials table
4. Create invoice list/create/view pages
5. Implement serial selection for serial-controlled items
6. Implement stock deduction on invoice issue
7. Implement serial update on invoice issue
8. Implement customer warranty update on invoice issue
9. Implement VAT auto-calculation