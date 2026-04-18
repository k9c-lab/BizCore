# Quotation to Invoice Conversion Specification

## Objective
Support creating Invoice from existing Quotation and support reference fields.

---

## 1. Invoice Header Changes

### Update: InvoiceHeaders
Add fields:
- QuotationId (int, nullable, FK to QuotationHeaders)
- ReferenceNo (string, nullable)

Purpose:
- QuotationId = internal reference to quotation in system
- ReferenceNo = external reference such as customer PO number or other document number

---

## 2. Quotation Header Changes

### Update: QuotationHeaders
Add field if not already exists:
- ReferenceNo (string, nullable)

---

## 3. Convert to Invoice Rules

When converting quotation to invoice:
1. Create invoice from selected quotation
2. Copy quotation header data:
   - CustomerId
   - SalespersonId
   - VatType
   - HeaderDiscountAmount
   - Remark
3. Copy quotation detail lines:
   - ItemId
   - Qty
   - UnitPrice
   - DiscountAmount
   - Description
   - LineTotal
4. Set:
   - InvoiceHeaders.QuotationId = QuotationId
5. Do not assign serial numbers during conversion
6. Do not deduct stock during conversion if invoice remains Draft
7. Serial selection and stock deduction happen later when invoice is issued

---

## 4. Reference Display

### Invoice screen / print
Show:
- ReferenceNo
- Related QuotationNo if QuotationId is not null

### Quotation screen / print
Show:
- ReferenceNo

---

## 5. Status Rules

### Quotation
- Draft
- Approved
- Converted
- Cancelled

After successful conversion:
- set quotation status = Converted

Note:
- For now assume one quotation converts to one invoice
- Do not implement partial conversion yet

---

## 6. Validation Rules

- Only approved quotation can be converted to invoice
- Converted quotation cannot be converted again
- Invoice created from quotation must preserve quotation customer
- Do not create duplicate conversion from same quotation

---

## 7. Deliverables for Codex

1. Update InvoiceHeaders table
2. Update QuotationHeaders table if needed
3. Add Convert to Invoice action
4. Create invoice draft from quotation
5. Show quotation reference in invoice view and print
6. Show ReferenceNo in quotation and invoice screens