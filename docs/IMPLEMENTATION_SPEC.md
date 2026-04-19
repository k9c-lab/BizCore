# Core / Master Module Specification

## Objective
Define all master data used across Sales, Purchasing, Inventory, Serial, and Warranty modules.

---

## 1. User & Role

### Roles
- Admin
- Sales
- Purchasing
- Accounting

---

### Table: Users
- UserId (int, PK)
- Username (string, unique)
- PasswordHash (string)
- FullName (string)
- Email (string)
- Role (string)
- IsActive (bool)
- CreatedDate (datetime)

---

## 2. Customer

### Table: Customers
- CustomerId (int, PK)
- CustomerCode (string, unique)
- CustomerName (string)
- TaxId (string)
- BranchNo (string)
- ContactName (string)
- Phone (string)
- Email (string)
- BillingAddress (string)
- ShippingAddress (string)
- CreditTermDays (int)
- IsActive (bool)
- CreatedDate (datetime)

---

## 3. Supplier

### Table: Suppliers
- SupplierId (int, PK)
- SupplierCode (string, unique)
- SupplierName (string)
- TaxId (string)
- BranchNo (string)
- ContactName (string)
- Phone (string)
- Email (string)
- Address (string)
- IsActive (bool)
- CreatedDate (datetime)

---

## 4. Salesperson

### Table: Salespersons
- SalespersonId (int, PK)
- SalespersonCode (string)
- SalespersonName (string)
- Phone (string)
- Email (string)
- IsActive (bool)

---

## 5. Item

### Table: Items
- ItemId (int, PK)
- ItemCode (string, unique)
- ItemName (string)
- PartNumber (string)
- ItemType (string: Product / Service)
- Unit (string)
- UnitPrice (decimal(18,2))
- TrackStock (bool)
- IsSerialControlled (bool)
- CurrentStock (decimal(18,2))
- IsActive (bool)
- CreatedDate (datetime)

---

## Business Rules

1. CustomerCode, SupplierCode, ItemCode must be unique
2. TrackStock = true → stock must be updated later
3. IsSerialControlled = true → must use Serial module (Phase 3)
4. Service items:
   - TrackStock = false
   - IsSerialControlled = false
5. Product items:
   - TrackStock = true

---

## Relationships

- Customer ↔ Invoice (1:N)
- Supplier ↔ PO (1:N)
- Item ↔ InvoiceDetail (1:N)
- Item ↔ Receiving (1:N)
- Salesperson ↔ Customer (1:N optional)

---

## Required Screens

- User Management
- Customer List / Create / Edit
- Supplier List / Create / Edit
- Item List / Create / Edit
- Salesperson List / Create / Edit

---

## Validation Rules

- Required fields must not be null
- Unique codes must be validated before insert
- Email format validation
- UnitPrice must be >= 0
- CurrentStock must be >= 0

---

## Deliverables for Codex

1. SQL Server Tables
2. Entity Models
3. DbContext
4. CRUD Controllers
5. Razor Views (List / Create / Edit)
6. Basic Validation