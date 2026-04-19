# Task: Cancel Payment

## Objective
Implement safe payment cancellation and invoice balance rollback.

## Requirements

1. Allow cancel only when Payment status = Posted

2. Block cancellation if an issued receipt exists for the payment
- show clear error message
- do not auto-cancel receipt

3. On cancel:
- reverse all PaymentAllocations from related invoices
- update:
  - InvoiceHeaders.PaidAmount
  - InvoiceHeaders.BalanceAmount
  - InvoiceHeaders.Status

4. Set PaymentHeaders.Status = Cancelled

5. Use database transaction

6. UI:
- Add Cancel action in Payment List/View
- Cancelled payment must be read-only

## Constraints
- Do not change receipt logic except blocking payment cancel when receipt exists
- Do not implement auto-reversal of receipt