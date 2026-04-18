# Task: Purchasing / Receiving Module

## Objective
Implement purchasing and receiving before sales invoice continues.

## Scope
1. Purchase Request
2. Purchase Order
3. Receiving
4. Stock increase on receiving
5. Serial creation on receiving for serial-controlled items

## Requirements

### Create database tables
- PurchaseRequestHeaders
- PurchaseRequestDetails
- PurchaseOrderHeaders
- PurchaseOrderDetails
- ReceivingHeaders
- ReceivingDetails
- ReceivingSerials

### Update existing logic
- Use existing Suppliers
- Use existing Items
- Use existing SerialNumbers
- Increase Items.CurrentStock when receiving is posted

### Serial rules
- If Item.IsSerialControlled = true
- Require serial numbers during receiving
- Create SerialNumbers records with:
  - Status = InStock
  - CurrentCustomerId = null
  - InvoiceId = null

### PO rules
- Track ReceivedQty in PurchaseOrderDetails
- Update PO Status:
  - Draft
  - Approved
  - PartiallyReceived
  - FullyReceived
  - Cancelled

### Constraints
- Do not implement AP yet
- Do not implement sales serial assignment yet
- Do not implement warranty logic yet
- Use transaction when posting receiving

## Output
- SQL files into /database
- Code into /src

## Suggested implementation order
1. PR tables/models/pages
2. PO tables/models/pages
3. Receiving tables/models/pages
4. Stock update logic
5. Serial creation logic