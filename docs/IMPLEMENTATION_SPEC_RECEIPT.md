# Receipt Module Specification

## Objective
Implement receipt (ใบเสร็จรับเงิน) from payment.

---

## 1. Receipt Header

### Table: ReceiptHeaders
- ReceiptId (int, PK)
- ReceiptNo (string, unique)
- ReceiptDate (datetime)
- CustomerId (int, FK)
- PaymentId (int, FK)
- TotalAmount (decimal(18,2))
- Remark (string, nullable)
- Status (string: Issued / Cancelled)
- CreatedDate (datetime)

---

## 2. Source Data

Use:
- PaymentHeaders
- PaymentAllocations
- InvoiceHeaders
- Customers

---

## 3. Rules

1. Receipt is created from Payment only
2. One Payment = One Receipt (for now)
3. Receipt amount = PaymentHeaders.Amount
4. Receipt is read-only after issue
5. Do not allow editing receipt

---

## 4. Running Number

Format:
- RC-YYYYMM-XXXX

---

## 5. Display Data

### Receipt must show:
- ReceiptNo
- ReceiptDate
- Customer info
- Payment info
- List of invoices paid:
  - InvoiceNo
  - AppliedAmount

---

## 6. Actions

### From Payment
- Add button: "Generate Receipt"

### After generate:
- Create ReceiptHeaders
- Link to PaymentId

---

## 7. Print Layout

- Similar to Quotation / Invoice
- Show:
  - Company info
  - Customer info
  - Payment amount
  - Invoice allocations
  - Total received

---

## 8. Validation

- Only Posted Payment can generate Receipt
- Do not allow duplicate receipt for same payment

---

## 9. Deliverables

1. Create ReceiptHeaders table
2. Create Receipt list / view page
3. Add Generate Receipt action in Payment
4. Create Receipt print page