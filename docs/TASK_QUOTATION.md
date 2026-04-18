# Task: Quotation Module

## Objective
Implement quotation workflow before invoice.

## Requirements

### Create database tables
- QuotationHeaders
- QuotationDetails

### Use existing tables
- Customers
- Items
- Salespersons

### Quotation behavior
1. Create quotation directly
2. Support VAT / NoVAT
3. Auto-calculate:
   - Subtotal
   - VatAmount
   - TotalAmount
4. Support status:
   - Draft
   - Approved
   - Converted
   - Cancelled

### Convert behavior
1. Add Convert to Invoice action
2. Create invoice draft from quotation
3. Copy quotation lines into invoice
4. Do not assign serials during conversion
5. Do not deduct stock during quotation

## Constraints
- Do not implement stock deduction in quotation
- Do not implement serial assignment in quotation
- Do not implement payment/receipt here

## Output
- SQL files into /database
- Code into /src