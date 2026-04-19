# Task: Purchase Order Standardization

## Objective
Standardize Purchase Order pages with searchable dropdowns, action buttons, validation feedback, and consistent layout.

## Requirements

1. Supplier
- Replace Supplier dropdown with searchable autocomplete
- Search by SupplierCode, SupplierName, TaxId
- Show supplier info after selection

2. Item
- Replace Item dropdown with searchable autocomplete
- Search by ItemCode, ItemName, PartNumber

3. Status flow
- Use action buttons instead of manual status editing
- Add/standardize:
  - Save Draft
  - Approve
  - Cancel PO
  - Receive Goods

4. Validation UX
- If button is disabled, show clear reason/message
- Use same validation style as Quotation/Invoice/Payment/Receipt

5. Layout
- Make PO pages follow same UI/UX standard as Quotation and Invoice

## Constraints
- Keep existing purchasing business logic
- UI/UX and workflow standardization only