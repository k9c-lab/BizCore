# Purchase Order Standard Specification

## Objective
Standardize Purchase Order UI/UX, searchable dropdowns, action buttons, validation messages, and status flow.

---

## Scope
- Purchase Orders/Create
- Purchase Orders/Edit
- Purchase Orders/Details
- Purchase Orders/List

---

## 1. Supplier Selection

### Requirements
1. Replace normal Supplier dropdown with searchable autocomplete dropdown
2. Search by:
- SupplierCode
- SupplierName
- TaxId

3. After selecting supplier, show read-only supplier information in consistent format:
- SupplierName
- TaxId
- ContactName
- Phone
- Email
- Address

---

## 2. Item Selection

### Requirements
1. Replace normal Item dropdown with searchable autocomplete dropdown
2. Search by:
- ItemCode
- ItemName
- PartNumber

3. After selecting item:
- show ItemName
- show PartNumber
- if ItemType = Product and TrackStock = true:
  - optionally show CurrentStock as small secondary text
- if ItemType = Service:
  - no stock/serial UI needed in PO unless already supported by your existing design

---

## 3. Purchase Order Status Flow

### Status
- Draft
- Approved
- PartiallyReceived
- FullyReceived
- Cancelled

### Rules
1. Status changes must happen by action buttons on screen
2. Do not rely on manually editing status field
3. Use consistent button placement and validation style

---

## 4. Action Buttons

### In Purchase Order pages
- Save Draft
- Approve
- Cancel PO
- Create Receiving / Receive Goods

### Behavior
1. Approve enabled only when PO is valid
2. Receive Goods enabled only when PO is Approved or PartiallyReceived
3. Cancel PO enabled only when allowed by business rules
4. If button is disabled:
- show clear reason/message

---

## 5. Validation / Disabled Reason

### Examples
- Approve disabled:
  - supplier not selected
  - no detail lines
  - invalid qty or unit price

- Receive Goods disabled:
  - PO not approved
  - PO already fully received
  - PO cancelled

- Cancel PO disabled:
  - receiving already exists and cancellation is not allowed by current rule
  - PO already cancelled

System must show visible reason if an action is disabled.

---

## 6. Layout Standard

Use the same visual style as:
- Quotation
- Invoice
- Payment
- Receipt

### Requirements
- consistent spacing
- consistent card layout
- consistent section titles
- action buttons at top
- customer/supplier info in readable card
- summary section aligned consistently

---

## 7. Deliverables for Codex

1. Supplier autocomplete
2. Item autocomplete
3. Action-button-based status flow
4. Disabled reason display
5. Standardized PO page layout