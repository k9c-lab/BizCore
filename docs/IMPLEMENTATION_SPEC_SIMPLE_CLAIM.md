# Simple Supplier Claim Specification

## Objective
Implement simple supplier claim tracking by serial number without claim document header/detail.

---

## Table: SerialClaimLogs
- SerialClaimLogId (int, PK)
- SerialId (int, FK)
- SupplierId (int, FK)
- ClaimDate (datetime)
- ProblemDescription (string, nullable)
- ClaimStatus (string: Open / Sent / Returned / Rejected / Closed)
- Remark (string, nullable)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## Source Data
Use existing SerialNumbers table.

SerialNumbers must contain:
- SerialId
- SerialNo
- ItemId
- SupplierId
- SupplierWarrantyStartDate
- SupplierWarrantyEndDate
- Status

---

## Business Rules
1. Claim must be created from existing serial
2. Claim page opens from selected serial only
3. Claim page is not a search page
4. ClaimDate must be within supplier warranty period
5. SupplierId in claim log must match serial's SupplierId
6. On successful claim creation:
   - create SerialClaimLogs record
   - update SerialNumbers.Status = ClaimedToSupplier
7. Do not use claim header/detail structure

---

## Required Screen
### Claim Page
Show read-only:
- SerialNo
- ItemCode
- ItemName
- PartNumber
- SupplierName
- SupplierWarrantyStartDate
- SupplierWarrantyEndDate
- Current Serial Status

Input:
- ClaimDate
- ProblemDescription
- ClaimStatus
- Remark

---

## Deliverables for Codex
1. Create SerialClaimLogs table
2. Refactor existing claim page to simple claim mode
3. Validation for supplier warranty
4. Update serial status after claim