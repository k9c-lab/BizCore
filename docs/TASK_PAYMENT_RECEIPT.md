# Task: Payment and Receipt Module

## Objective
Implement payment receiving and receipt issuance for existing invoices.

## Requirements

### Create database tables
- PaymentHeaders
- PaymentAllocations
- ReceiptHeaders

### Use existing tables
- Customers
- InvoiceHeaders

### Payment behavior
1. Create payment for one customer
2. Allocate payment to one or multiple invoices
3. On payment post:
   - update InvoiceHeaders.PaidAmount
   - update InvoiceHeaders.BalanceAmount
   - update InvoiceHeaders.Status

### Invoice status behavior
- Issued
- PartiallyPaid
- Paid

### Receipt behavior
1. Create receipt from posted payment
2. One payment = one receipt for now
3. Receipt must be read-only after issue

## Constraints
- Do not implement quotation yet
- Do not implement AP yet
- Do not implement receipt cancellation reversal yet
- Do not implement credit note/debit note yet

## Output
- SQL files into /database
- Code into /src