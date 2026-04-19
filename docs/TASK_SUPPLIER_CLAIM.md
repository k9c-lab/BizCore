# Task: Supplier Claim Module

## Objective
Implement Supplier Claim module using existing SerialNumbers and Supplier warranty data.

## Scope
1. SupplierClaimHeaders
2. SupplierClaimDetails
3. Claim List page
4. Claim Create page
5. Claim Detail/View page
6. Validation for supplier warranty and duplicate open claims

## Requirements

### Create database tables
- SupplierClaimHeaders
- SupplierClaimDetails

### Use existing tables
- Suppliers
- Items
- SerialNumbers

### Validation
- Serial must have SupplierId
- ClaimDate must be within supplier warranty period
- Do not allow same serial to have multiple open claims

### Status
- Draft
- SentToSupplier
- Approved
- Rejected
- Returned
- Closed
- Cancelled

## Constraints
- Do not implement replacement stock yet
- Do not implement receive-back from supplier yet
- Do not implement customer claim yet

## Output
- SQL files into /database
- Code into /src