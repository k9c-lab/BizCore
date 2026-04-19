# Purchase Order Print Specification

## Objective
Implement Purchase Order print / PDF layout in the same document style as Quotation and Invoice.

---

## 1. Print View

Create a dedicated print-friendly Purchase Order view:
- A4 layout
- clean business document style
- suitable for browser print and PDF export

---

## 2. Header Section

Show seller/company information:
- CompanyName
- CompanyAddress
- CompanyTaxId
- Phone (if available)
- Email (if available)

Show document info on the right:
- Purchase Order title
- PONo
- PODate
- Status
- ReferenceNo (if exists)
- ExpectedReceiveDate (if exists)

---

## 3. Supplier Section

Show supplier information clearly:
- SupplierName
- Address
- TaxId
- ContactName
- Phone
- Email

---

## 4. Detail Table

Columns:
- No
- Description
- Qty
- UnitPrice
- LineTotal

Requirements:
- Description must support multi-line text
- Long text must wrap correctly
- Numeric columns right-aligned

---

## 5. Summary Section

Place summary below detail table and align to the right:
- Subtotal
- Discount
- VAT
- TotalAmount

TotalAmount should be visually prominent.

---

## 6. Remark Section

Use existing Remark field:
- multi-line
- preserve line breaks

---

## 7. Signature Section

Add signature placeholders:
- Requester / Buyer
- Approver / Authorized Signature
- Date

---

## 8. Print Behavior

1. Add Print action from PO View/List
2. Open print-friendly page
3. Auto trigger browser print dialog using window.print()
4. Hide non-document UI elements
5. Use print-specific CSS if needed

---

## 9. Deliverables for Codex

1. Create PO print page
2. Add Print action
3. Support A4 layout
4. Auto-open print dialog