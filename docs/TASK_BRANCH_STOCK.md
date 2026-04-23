# Task: Branch-Based Inventory Foundation

## Objective
Add branch-based inventory foundation so stock, serials, users, and future documents can be separated by branch.

Current system is single-stock-pool. The new direction is:

- 1 Branch = 1 Stock Location
- No Warehouse table in the first phase
- Serial can belong to only one branch at a time
- Normal users should see only their own branch data
- Admin can access all branches

---

## Phase 1: Branch Foundation

1. Create `Branches` table
2. Add branch master pages
   - List
   - Create
   - Edit
   - Details
3. Add branch fields to users
   - `Users.BranchId`
   - `Users.CanAccessAllBranches`
4. Add default branch
   - `MAIN`
   - `Main Branch`
5. Existing users should be assigned to `MAIN`
6. Admin users should be allowed to access all branches
7. Show branch in user management
8. Add branch claim at login
9. Add Branches menu under Master Data

## Phase 1 Constraints
- Do not change stock movement logic yet
- Do not filter sales/purchasing/inventory documents by branch yet
- Do not add stock transfer yet
- Do not add stock issue yet

---

## Future Phases

### Phase 2: Branch Stock Foundation
- Add `SerialNumbers.BranchId`
- Add `StockBalances`
- Add `StockMovements`
- Migrate existing serials/stock to `MAIN`
- Update Stock Inquiry / Serial Inquiry branch filtering

### Phase 3: Purchasing / Receiving by Branch
- Add `BranchId` to PO/Receiving
- Post receiving into branch stock

### Phase 4: Sales / Invoice by Branch
- Add `BranchId` to invoices
- Select serials only from invoice branch
- Deduct branch stock on issue

### Phase 5: Stock Transfer
- Transfer stock between branches
- Support serial-controlled items

### Phase 6: Stock Issue
- Issue stock for internal use/demo/service
- Support serial-controlled items

### Phase 7: Claim by Branch
- Customer Claim branch
- Supplier Claim branch
- Replacement serial from same branch
