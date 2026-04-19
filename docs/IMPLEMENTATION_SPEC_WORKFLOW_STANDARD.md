# Workflow Standard Specification

## Objective
Standardize action buttons, status flow, validation, and disabled-button explanation across Quotation, Invoice, Payment, and Receipt.

---

## Scope
- Quotation
- Invoice
- Payment
- Receipt

---

## General Rules

1. Status changes must be triggered by action buttons on screen
- Do not rely on manually editing status fields

2. If an action is not allowed:
- disable the button
- show clear reason/message

3. Use consistent action button placement across pages

4. Use consistent validation feedback style across modules

---

## Quotation

### Actions
- Save Draft
- Approve
- Convert to Invoice

### Rules
- Convert to Invoice enabled only when quotation is approved
- If disabled, show reason

---

## Invoice

### Actions
- Save Draft
- Issue Invoice
- Cancel Invoice
- Receive Payment

### Rules
- Issue Invoice enabled only when required data is complete
- For serial-controlled products:
  - serial selection must be complete
- If disabled, show reason
- Cancel Invoice blocked if payment exists

---

## Payment

### Actions
- Save Draft
- Post Payment
- Generate Receipt
- Cancel Payment

### Rules
- Post Payment enabled only when allocation is complete
- Generate Receipt enabled only when payment is posted
- Cancel Payment blocked if issued receipt exists
- If disabled, show reason

---

## Receipt

### Actions
- Print
- Cancel Receipt

### Rules
- Cancel Receipt only when status = Issued
- If disabled, show reason

---

## Deliverables
1. Standardize action buttons across modules
2. Standardize disabled-button explanation
3. Standardize validation behavior
4. Remove manual status editing workflow where still present