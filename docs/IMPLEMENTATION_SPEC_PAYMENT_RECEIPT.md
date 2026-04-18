# Payment and Receipt Module Specification

## Objective
Implement customer payment receiving and receipt issuance for existing sales invoices.

---

## 1. Payment Header

### Table: PaymentHeaders
- PaymentId (int, PK)
- PaymentNo (string, unique)
- PaymentDate (datetime)
- CustomerId (int, FK)
- PaymentMethod (string: Cash / Transfer / Cheque / Other)
- ReferenceNo (string, nullable)
- Amount (decimal(18,2))
- Remark (string, nullable)
- Status (string: Draft / Posted / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## 2. Payment Allocation

### Table: PaymentAllocations
- PaymentAllocationId (int, PK)
- PaymentId (int, FK)
- InvoiceId (int, FK)
- AppliedAmount (decimal(18,2))

---

## 3. Receipt Header

### Table: ReceiptHeaders
- ReceiptId (int, PK)
- ReceiptNo (string, unique)
- ReceiptDate (datetime)
- CustomerId (int, FK)
- PaymentId (int, FK)
- TotalReceivedAmount (decimal(18,2))
- Remark (string, nullable)
- Status (string: Issued / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## 4. Source Data

Use existing tables:
- Customers
- InvoiceHeaders

InvoiceHeaders already contain:
- InvoiceId
- CustomerId
- TotalAmount
- PaidAmount
- BalanceAmount
- Status

---

## 5. Payment Rules

1. Payment is created for one customer only
2. One payment can be allocated to one or many invoices of the same customer
3. AppliedAmount must be > 0
4. Sum of all AppliedAmount must be <= PaymentHeaders.Amount
5. AppliedAmount for each invoice must not exceed that invoice BalanceAmount
6. Only active invoices of the same customer can be selected
7. On payment post:
   - update InvoiceHeaders.PaidAmount
   - update InvoiceHeaders.BalanceAmount
   - update InvoiceHeaders.Status

---

## 6. Invoice Status Rules

### If payment posted:
- BalanceAmount = TotalAmount - PaidAmount

### Status logic
- If PaidAmount = 0 and invoice is active:
  - Status = Issued
- If PaidAmount > 0 and BalanceAmount > 0:
  - Status = PartiallyPaid
- If BalanceAmount = 0:
  - Status = Paid

---

## 7. Receipt Rules

1. Receipt is created from posted payment
2. One payment has one receipt for now
3. Receipt amount = PaymentHeaders.Amount or total applied amount, depending on chosen rule
4. Receipt is read-only after issue
5. Do not implement receipt cancellation reversal yet

Recommended rule now:
- Receipt total = PaymentHeaders.Amount

---

## 8. Running Number

Need running numbers for:
- PaymentNo
- ReceiptNo

Format examples:
- PAY-202604-0001
- RC-202604-0001

---

## 9. Required Screens

### Payment List
- PaymentNo
- PaymentDate
- Customer
- Amount
- Status

### Payment Create
- Select Customer
- PaymentDate
- PaymentMethod
- ReferenceNo
- Amount
- Remark
- Select invoices for allocation
- Input AppliedAmount per invoice

### Payment View
- Header information
- Allocation list

### Receipt List
- ReceiptNo
- ReceiptDate
- Customer
- TotalReceivedAmount
- Status

### Receipt View
- Receipt header
- Related payment
- Allocated invoices

---

## 10. Validation Rules

- Customer is required
- Amount > 0
- Must have at least 1 payment allocation
- Sum of AppliedAmount must be <= Payment amount
- AppliedAmount must not exceed invoice BalanceAmount
- Payment invoice customer must match payment customer
- Do not allow payment against cancelled invoice

---

## 11. Deliverables for Codex

1. Create PaymentHeaders table
2. Create PaymentAllocations table
3. Create ReceiptHeaders table
4. Create payment list/create/view pages
5. Create receipt list/view pages
6. Implement invoice balance update on payment post
7. Implement invoice status update
8. Implement running number