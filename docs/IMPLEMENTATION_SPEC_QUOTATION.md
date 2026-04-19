# Quotation Module Specification

## Objective
Implement quotation workflow for sales before invoice creation.

---

## 1. Quotation Header

### Table: QuotationHeaders
- QuotationId (int, PK)
- QuotationNo (string, unique)
- QuotationDate (datetime)
- CustomerId (int, FK)
- SalespersonId (int, nullable, FK)
- Remark (string, nullable)
- Subtotal (decimal(18,2))
- DiscountAmount (decimal(18,2))
- VatType (string: VAT / NoVAT)
- VatAmount (decimal(18,2))
- TotalAmount (decimal(18,2))
- Status (string: Draft / Approved / Converted / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## 2. Quotation Detail

### Table: QuotationDetails
- QuotationDetailId (int, PK)
- QuotationId (int, FK)
- ItemId (int, FK)
- Qty (decimal(18,2))
- UnitPrice (decimal(18,2))
- DiscountAmount (decimal(18,2))
- LineTotal (decimal(18,2))
- Remark (string, nullable)

---

## 3. Source Data

Use existing tables:
- Customers
- Items
- Salespersons

---

## 4. Quotation Rules

1. Quotation does not deduct stock
2. Quotation does not assign serial numbers
3. Quotation is only a pricing/sales document
4. User selects:
   - Customer
   - Salesperson
   - VatType
5. System calculates:
   - Subtotal
   - VatAmount
   - TotalAmount
6. User must not manually input VatAmount
7. Quotation can be edited while status = Draft
8. Approved quotation can be converted to invoice
9. Converted quotation status = Converted

---

## 5. Convert to Invoice

When converting quotation to invoice:
1. Create invoice draft from quotation
2. Copy:
   - CustomerId
   - SalespersonId
   - VatType
   - detail lines
   - pricing values
3. Do not assign serials during conversion
4. Serial selection happens later in Invoice Create/Edit
5. Quotation status becomes Converted after successful conversion

---

## 6. VAT Rules

1. User selects:
   - VAT
   - NoVAT
2. System auto-calculates:
   - Subtotal
   - VatAmount
   - TotalAmount

---

## 7. Running Number

Format:
- QT-YYYYMM-XXXX

Example:
- QT-202604-0001

Reset running every month.

---

## 8. Required Screens

### Quotation List
- QuotationNo
- QuotationDate
- Customer
- TotalAmount
- Status

### Quotation Create
- Select Customer
- Select Salesperson
- Select VatType
- Add item lines
- Auto-calculate totals

### Quotation Edit
- Allow edit while Draft

### Quotation View
- Show header and detail lines

### Convert to Invoice
- Action from Quotation View/List
- Create invoice draft from quotation

---

## 9. Validation Rules

- Customer required
- Must have at least 1 detail line
- Qty > 0
- UnitPrice >= 0
- Quotation with status Converted cannot be edited as normal draft
- Do not allow duplicate QuotationNo

---

## 10. Deliverables for Codex

1. Create QuotationHeaders table
2. Create QuotationDetails table
3. Create quotation list/create/edit/view pages
4. Implement VAT auto-calculation
5. Implement running number
6. Implement Convert to Invoice action
7. Keep stock and serial logic out of quotation