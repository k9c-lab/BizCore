# Branch-Based Inventory Implementation Specification

## Objective
Introduce branch foundation before converting inventory to branch-based stock.

The first implementation phase keeps stock logic unchanged and adds branch/user structure only.

---

## Design Decision

Use `Branch` as the first stock-location concept.

Do not create a separate `Warehouse` table yet.

Current assumption:

```text
1 Branch = 1 Stock Location
```

Future warehouse support can be added later if one branch needs multiple stock locations.

---

## Phase 1 Tables

### Branches
- BranchId int identity PK
- BranchCode nvarchar(30), unique, required
- BranchName nvarchar(150), required
- Address nvarchar(500), nullable
- PhoneNumber nvarchar(50), nullable
- Email nvarchar(256), nullable
- IsActive bit, required, default 1
- CreatedDate datetime2, required

### Users additions
- BranchId int nullable FK Branches
- CanAccessAllBranches bit required default 0

Rules:
- Existing users are assigned to default branch `MAIN`
- Existing Admin users get `CanAccessAllBranches = 1`
- Non-admin users should normally have one assigned branch

---

## Authentication Claims

Add login claims:
- `BranchId`
- `CanAccessAllBranches`

These claims will support future branch filtering.

---

## UI

### Branch Master
Add admin-only Branch master pages:
- Index
- Create
- Edit
- Details

Search by:
- BranchCode
- BranchName
- PhoneNumber
- Email

Filter:
- Active
- Inactive

### User Management
Show and edit:
- Branch
- Can Access All Branches

Admin users may have all-branch access. Other users should normally be scoped to their branch.

---

## SQL Script

Next script:
- `database/026_branch_foundation.sql`

The script must be safe to run multiple times.

---

## Phase 1 Constraints

Do not modify:
- PO stock logic
- Receiving stock logic
- Invoice stock logic
- Claim stock logic
- Stock inquiry calculations

---

## Verification

1. Run SQL 026
2. Login as admin
3. Open Branches menu
4. Confirm `MAIN` branch exists
5. Open User Management
6. Create/Edit user with branch assignment
7. Confirm login still works
8. Build must pass
