# Task: Invoice Status and Cancel

## Objective
Implement invoice status control, quotation link display, and safe invoice cancellation.

## Requirements

1. Invoice edit rules
- Only Draft invoice can be edited
- Issued invoice cannot be edited
- Cancelled invoice cannot be edited

2. Invoice cancellation
- Allow cancel Draft invoice
- Allow cancel Issued invoice only if no payment exists
- If payment exists:
  - block cancel
  - show clear error message

3. On cancel Issued invoice:
- restore Items.CurrentStock for stock items
- update related SerialNumbers:
  - Status = InStock
  - CurrentCustomerId = null
  - InvoiceId = null
  - CustomerWarrantyStartDate = null
  - CustomerWarrantyEndDate = null
- set Invoice status = Cancelled

4. Quotation link
- Show related QuotationNo in Invoice View if QuotationId exists

5. Constraints
- Do not reopen quotation status
- Do not implement credit note/debit note
- Use database transaction for cancel logic