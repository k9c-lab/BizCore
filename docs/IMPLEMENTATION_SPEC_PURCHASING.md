# Purchasing / Receiving Module Specification

## Objective
Implement purchasing flow for:
- Purchase Request (PR)
- Purchase Order (PO)
- Receiving
- Initial stock-in
- Initial serial creation

This module must prepare stock and serial data before sales invoice uses them.

---

## 1. Purchase Request

### Table: PurchaseRequestHeaders
- PurchaseRequestId (int, PK)
- PRNo (string, unique)
- PRDate (datetime)
- RequestByUserId (int)
- Remark (string, nullable)
- Status (string: Draft / Approved / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

### Table: PurchaseRequestDetails
- PurchaseRequestDetailId (int, PK)
- PurchaseRequestId (int, FK)
- ItemId (int, FK)
- Qty (decimal(18,2))
- UnitPrice (decimal(18,2), nullable)
- Remark (string, nullable)

---

## 2. Purchase Order

### Table: PurchaseOrderHeaders
- PurchaseOrderId (int, PK)
- PONo (string, unique)
- PODate (datetime)
- SupplierId (int, FK)
- PurchaseRequestId (int, nullable, FK)
- ReferenceNo (string, nullable)
- ExpectedReceiveDate (datetime, nullable)
- Remark (string, nullable)
- Subtotal (decimal(18,2))
- DiscountAmount (decimal(18,2))
- VatAmount (decimal(18,2))
- TotalAmount (decimal(18,2))
- Status (string: Draft / Approved / PartiallyReceived / FullyReceived / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

### Table: PurchaseOrderDetails
- PurchaseOrderDetailId (int, PK)
- PurchaseOrderId (int, FK)
- ItemId (int, FK)
- Qty (decimal(18,2))
- ReceivedQty (decimal(18,2))
- UnitPrice (decimal(18,2))
- DiscountAmount (decimal(18,2))
- LineTotal (decimal(18,2))
- Remark (string, nullable)

---

## 3. Receiving

### Table: ReceivingHeaders
- ReceivingId (int, PK)
- ReceivingNo (string, unique)
- ReceiveDate (datetime)
- SupplierId (int, FK)
- PurchaseOrderId (int, FK)
- DeliveryNoteNo (string, nullable)
- Remark (string, nullable)
- Status (string: Draft / Posted / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

### Table: ReceivingDetails
- ReceivingDetailId (int, PK)
- ReceivingId (int, FK)
- PurchaseOrderDetailId (int, FK)
- ItemId (int, FK)
- QtyReceived (decimal(18,2))
- Remark (string, nullable)

---

## 4. Receiving Serial

Use this table only when item is serial-controlled.

### Table: ReceivingSerials
- ReceivingSerialId (int, PK)
- ReceivingDetailId (int, FK)
- ItemId (int, FK)
- SerialNo (string)
- CreatedDate (datetime)

Rules:
- SerialNo must be unique
- Number of serial records must equal QtyReceived for serial-controlled item

---

## 5. Serial Integration

Existing table:
### SerialNumbers
- SerialId
- ItemId
- SerialNo
- Status
- CurrentCustomerId
- InvoiceId
- WarrantyStartDate
- WarrantyEndDate
- CreatedDate

When receiving is posted:
- Create SerialNumbers records for serial-controlled items
- Status = InStock
- CurrentCustomerId = null
- InvoiceId = null
- Warranty dates = null

---

## 6. Stock Update Rules

When receiving is posted:
- If Item.TrackStock = true
- Increase Items.CurrentStock by QtyReceived

When receiving is cancelled:
- Only allow cancel if stock and serial are still reversible
- For now, cancellation can be blocked if already used later

---

## 7. Running Number

Need running numbers for:
- PRNo
- PONo
- ReceivingNo

Format examples:
- PR-202604-0001
- PO-202604-0001
- GR-202604-0001

---

## 8. Business Rules

1. PR is optional but supported
2. PO can be created directly without PR
3. Receiving must reference a PO
4. Cannot receive more than remaining PO quantity unless explicitly allowed
5. PO detail ReceivedQty must be updated after receiving is posted
6. If all PO lines fully received:
   - PO Status = FullyReceived
7. If some lines received:
   - PO Status = PartiallyReceived
8. For serial-controlled item:
   - Receiving must capture serial numbers
9. For non-serial-controlled item:
   - No serial entry required
10. Receiving post must use transaction

---

## 9. Relationships

- Suppliers (1) -> (many) PurchaseOrderHeaders
- PurchaseRequestHeaders (1) -> (many) PurchaseRequestDetails
- PurchaseOrderHeaders (1) -> (many) PurchaseOrderDetails
- PurchaseOrderHeaders (1) -> (many) ReceivingHeaders
- ReceivingHeaders (1) -> (many) ReceivingDetails
- ReceivingDetails (1) -> (many) ReceivingSerials
- Items (1) -> (many) PurchaseRequestDetails
- Items (1) -> (many) PurchaseOrderDetails
- Items (1) -> (many) ReceivingDetails
- Items (1) -> (many) SerialNumbers

---

## 10. Required Screens

### PR
- PR List
- PR Create
- PR Edit
- PR View

### PO
- PO List
- PO Create
- PO Edit
- PO View

### Receiving
- Receiving List
- Receiving Create
- Receiving View

### Serial Entry in Receiving
- For serial-controlled item, allow entering serial numbers per received quantity

---

## 11. Validation Rules

### PR
- Must have at least 1 detail row
- Qty > 0

### PO
- Must select supplier
- Must have at least 1 detail row
- Qty > 0
- UnitPrice >= 0

### Receiving
- Must reference PO
- QtyReceived > 0
- QtyReceived must not exceed remaining qty
- Serial count must match QtyReceived for serial-controlled item
- SerialNo must be unique

---

## 12. Deliverables for Codex

1. SQL Server tables
2. Entity models
3. DbContext updates
4. CRUD pages for PR and PO
5. Receiving pages
6. Receiving post logic
7. Stock increase logic
8. Serial creation logic
9. Running number support