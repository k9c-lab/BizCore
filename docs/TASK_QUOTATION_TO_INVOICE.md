# Task: Quotation to Invoice Conversion

## Objective
Implement quotation to invoice conversion and reference fields.

## Requirements

1. Update InvoiceHeaders:
- add QuotationId
- add ReferenceNo

2. Update QuotationHeaders:
- add ReferenceNo if not already exists

3. Add Convert to Invoice action:
- only for approved quotation
- create invoice draft from quotation
- copy quotation lines
- set InvoiceHeaders.QuotationId

4. Quotation status:
- set to Converted after successful conversion

5. Display references:
- show ReferenceNo
- show related QuotationNo in invoice if QuotationId exists

## Constraints
- Do not implement partial conversion
- Do not assign serials during conversion
- Do not deduct stock during conversion unless invoice issue logic already handles it separately