# Supplier Claim Module Specification

## Objective
Track product claims to supplier by serial number within supplier warranty period.

---

## 1. Supplier Claim Header

### Table: SupplierClaimHeaders
- ClaimId (int, PK)
- ClaimNo (string, unique)
- ClaimDate (datetime)
- SupplierId (int, FK)
- Remark (string, nullable)
- Status (string: Draft / SentToSupplier / Approved / Rejected / Returned / Closed / Cancelled)
- CreatedDate (datetime)
- UpdatedDate (datetime, nullable)

---

## 2. Supplier Claim Detail

### Table: SupplierClaimDetails
- ClaimDetailId (int, PK)
- ClaimId (int, FK)
- SerialId (int, FK)
- ItemId (int, FK)
- ProblemDescription (string, nullable)
- ResultRemark (string, nullable)

---

## 3. Source Data

Claim must be created from existing SerialNumbers.

SerialNumbers must already contain:
- SerialId
- ItemId
- SerialNo
- SupplierId
- SupplierWarrantyStartDate
- SupplierWarrantyEndDate
- Status

---

## 4. Business Rules

1. Claim must reference existing serial number
2. Serial must have SupplierId
3. Claim date must be within supplier warranty period
4. Same serial cannot have multiple open claims at the same time
5. Supplier in claim must match supplier in serial record
6. ItemId in claim detail must match serial's ItemId
7. Cancelled and Closed claims are not considered open claims

---

## 5. Open Claim Status

Treat these as open claim statuses:
- Draft
- SentToSupplier
- Approved
- Returned

Do not allow same SerialId to appear in another open claim

---

## 6. Required Screens

### Claim List
- ClaimNo
- ClaimDate
- Supplier
- Status

### Claim Create
- Select Supplier
- Search and select serial number
- Show:
  - SerialNo
  - ItemCode
  - ItemName
  - PartNumber
  - SupplierWarrantyStartDate
  - SupplierWarrantyEndDate
  - Warranty status
- Input:
  - ProblemDescription

### Claim View / Detail
- Header information
- Claimed serial list
- Status update
- ResultRemark

---

## 7. Validation Rules

- Serial must exist
- Serial must belong to selected supplier
- ClaimDate must be <= SupplierWarrantyEndDate
- Cannot create duplicate open claim for same serial
- Claim must have at least 1 detail row

---

## 8. Deliverables for Codex

1. SQL Server tables
2. Entity models
3. DbContext updates
4. Claim list page
5. Claim create page
6. Claim detail/view page
7. Validation logic
8. Running number for ClaimNo