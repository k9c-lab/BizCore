# Receipt Cancel Specification

## Objective
Implement receipt cancellation as document cancellation only.

---

## 1. Receipt Status Rules

### ReceiptHeaders.Status
- Issued
- Cancelled

Rules:
1. Issued receipt can be cancelled
2. Cancelled receipt is read-only
3. Cancelled receipt remains visible in history

---

## 2. Cancel Receipt Rules

### Preconditions
1. Only Issued receipt can be cancelled

### On successful receipt cancellation
1. Set ReceiptHeaders.Status = Cancelled
2. Do not change PaymentHeaders
3. Do not change InvoiceHeaders
4. Do not reverse payment allocation

---

## 3. Required Screens / Actions

### Receipt List
- Show Status
- Add Cancel action only when allowed

### Receipt View
- Show receipt information
- If status = Issued:
  - allow Cancel
- If status = Cancelled:
  - read-only

---

## 4. Validation Rules

1. Only Issued receipt can be cancelled
2. Cancelled receipt cannot be cancelled again
3. Receipt cancel must not affect payment or invoice balances

---

## 5. Deliverables for Codex

1. Implement receipt cancel action
2. Update receipt status to Cancelled
3. Make cancelled receipt read-only
4. Keep cancelled receipt visible in history