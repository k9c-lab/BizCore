# Task: Invoice Module

## Objective
Implement direct sales invoice with serial selection and stock deduction.

## Requirements

### Create database tables
- InvoiceHeaders
- InvoiceDetails
- InvoiceSerials

### Use existing tables
- Customers
- Items
- Salespersons
- SerialNumbers

### Invoice behavior
1. Create invoice directly without quotation
2. Support VAT / NoVAT
3. Auto-calculate:
   - Subtotal
   - VatAmount
   - TotalAmount
   - BalanceAmount

### Serial behavior
1. For serial-controlled item:
   - user must select serials
   - serial count must equal Qty
   - only Status = InStock can be selected
2. On invoice issue:
   - update selected serials to Sold
   - assign CustomerId and InvoiceId
   - set customer warranty dates

### Stock behavior
- If Item.TrackStock = true
- deduct CurrentStock on invoice issue

## Constraints
- Do not implement quotation yet
- Do not implement receipt/payment yet
- Do not implement invoice cancellation reversal yet

## Output
- SQL files into /database
- Code into /src