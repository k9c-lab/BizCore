# Payment Cancel Specification

## Objective
Implement safe payment cancellation with proper invoice balance rollback.

---

## 1. Payment Status Rules

### PaymentHeaders.Status
- Draft
- Posted
- Cancelled

Rules:
1. Draft payment can be edited
2. Posted payment cannot be edited directly
3. Cancelled payment is read-only

---

## 2. Cancel Payment Rules

### Preconditions
1. Only Posted payment can be cancelled
2. If receipt already exists for this payment and receipt status = Issued:
   - do not allow payment cancellation
   - show message:
     "Cannot cancel payment because receipt already exists. Cancel receipt first."

### On successful payment cancellation
For each PaymentAllocation linked to the payment:
1. Find related InvoiceHeader
2. Reverse applied amount:
   - InvoiceHeaders.PaidAmount = InvoiceHeaders.PaidAmount - AppliedAmount
   - InvoiceHeaders.BalanceAmount = InvoiceHeaders.TotalAmount - InvoiceHeaders.PaidAmount
3. Recalculate InvoiceHeaders.Status:
   - If PaidAmount = 0 → Issued
   - If PaidAmount > 0 and BalanceAmount > 0 → PartiallyPaid
   - If BalanceAmount = 0 → Paid

After all allocations reversed:
4. Set PaymentHeaders.Status = Cancelled

---

## 3. Receipt Interaction

1. If payment has an issued receipt:
   - payment cannot be cancelled directly
2. Receipt must be cancelled first
3. For now:
   - do not auto-cancel receipt when cancelling payment

---

## 4. Transaction Rule

Payment cancellation must run inside one database transaction.

If any invoice rollback fails:
- rollback all changes
- payment must remain Posted

---

## 5. Required Screens / Actions

### Payment List
- Show Status
- Add Cancel action only when allowed

### Payment View
- Show:
  - PaymentNo
  - PaymentDate
  - Customer
  - Amount
  - Status
  - Allocated invoices
- If Posted and no issued receipt:
  - allow Cancel

### Cancel UX
- Show confirmation before cancelling
- If blocked due to receipt:
  - show clear message

---

## 6. Validation Rules

1. Only Posted payment can be cancelled
2. Draft payment does not need cancel flow
3. Cancelled payment cannot be cancelled again
4. Payment with issued receipt cannot be cancelled
5. Reverse all invoice allocations correctly
6. Use transaction

---

## 7. Deliverables for Codex

1. Implement payment cancel action
2. Roll back PaidAmount and BalanceAmount on invoices
3. Recalculate invoice status
4. Block cancel when issued receipt exists
5. Make cancelled payment read-only