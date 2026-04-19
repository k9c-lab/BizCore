# Task: Cancel Receipt

## Objective
Implement receipt cancellation without reversing payment logic.

## Requirements

1. Allow cancel only when Receipt status = Issued
2. On cancel:
- set ReceiptHeaders.Status = Cancelled
3. Do not change:
- PaymentHeaders
- PaymentAllocations
- InvoiceHeaders

4. UI:
- Add Cancel action in Receipt List/View
- Cancelled receipt must be read-only
- Keep cancelled receipt visible in history

## Constraints
- Do not reverse payment
- Do not reverse invoice balance