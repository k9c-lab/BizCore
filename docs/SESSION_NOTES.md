# Session Notes

## Project
- App name: `BizCore`
- App path: `src/AccountingSystem`
- Project file: `src/AccountingSystem/BizCore.csproj`
- Database name in current setup: `BizCoreDB`

## Current Status
- Core module is working
- Master data CRUD is in place for:
  - Customers
  - Suppliers
  - Items
  - Salespersons
- Product structure was refactored:
  - `Items` is product master at part-number level
  - `SerialNumbers` tracks physical units
  - supplier/customer warranty fields are split
- Sales modules currently implemented:
  - Quotation
  - Direct Invoice
  - Payment
  - Receipt
- Purchasing modules currently implemented:
  - Purchase Order
  - Receiving
  - Stock update on receiving post
  - Serial creation on receiving post for serial-controlled items
- Supplier claim was simplified:
  - simple serial-based supplier claim
  - no header/detail claim document workflow
- Inquiry pages completed:
  - Stock Inquiry
  - Stock Inquiry serial detail page
  - Serial Inquiry

## Important Functional Behavior
- Auto code generation with prefixes:
  - Customers: `CUS-0001`
  - Suppliers: `SUP-0001`
  - Salespersons: `SAL-0001`
  - Items: `ITM-0001`
- Purchase Order VAT:
  - `VatType` = `VAT` or `NoVAT`
  - `VatAmount` auto-calculated at 7%
- Quotation:
  - monthly running no. format `QT-YYYYMM-XXXX`
  - VAT auto-calculation
  - discount mode supports `Line` or `Header`
  - optional `ExpiryDate`
  - searchable customer/item dropdowns
  - print/PDF page exists
- Invoice:
  - monthly running no. format `INV-YYYYMM-XXXX`
  - supports `Product` and `Service` items
  - serial selection for serial-controlled products
  - stock deduction happens on direct invoice issue
  - print/PDF page now exists
- Payment / Receipt:
  - payment allocation to invoices
  - invoice balance/status update
  - receipt auto-created from payment
- Supplier claim:
  - claim starts from selected serial only
  - warranty validation blocks expired claims
  - save inserts into `SerialClaimLogs`
  - save updates `SerialNumbers.Status = ClaimedToSupplier`

## Inquiry Pages
- `StockInquiry/Index`
  - search by `ItemCode`, `ItemName`, `PartNumber`
- `StockInquiry/Serials/{itemId}`
  - shows serials for one selected item
  - has `Claim` action per serial
- `SerialInquiry/Index`
  - card-based layout
  - search by `SerialNo`, `ItemCode`, `PartNumber`
  - has `Claim` action per serial

## SQL Scripts In Repo
- `database/001_core_module.sql`
- `database/002_core_seed_data.sql`
- `database/003_sales_quotation.sql`
- `database/004_core_product_structure_update.sql`
- `database/005_purchasing_po_receiving.sql`
- `database/006_purchase_order_vat_type_update.sql`
- `database/007_serial_warranty_split_update.sql`
- `database/008_supplier_claim_module.sql`
- `database/009_simple_supplier_claim_refactor.sql`
- `database/010_invoice_module.sql`
- `database/011_payment_receipt_module.sql`
- `database/012_quotation_module_update.sql`
- `database/013_service_item_seed.sql`
- `database/014_quotation_to_invoice_reference_update.sql`

## Latest Work Done Today
- Implemented quotation-to-invoice reference support
- Added `QuotationHeaders.ReferenceNo`
- Added `InvoiceHeaders.QuotationId`
- Added `InvoiceHeaders.ReferenceNo`
- Updated quotation create/edit/details/index/print to show `ReferenceNo`
- Updated quotation conversion logic:
  - only approved quotation converts
  - duplicate conversion is blocked
  - invoice draft is created from quotation
  - quotation status becomes `Converted`
- Updated invoice screens to show:
  - related `QuotationNo`
  - `ReferenceNo`
- Added invoice print page:
  - `Views/Invoices/Print.cshtml`

## Scripts To Run On Existing Database
- If not already applied, run:
  - `database/014_quotation_to_invoice_reference_update.sql`

## Build / Verification Notes
- Today’s final verification was not fully completed because `BizCore.exe` was running and locking build output
- A normal `dotnet build BizCore.csproj` should be run after stopping the running app
- No code-level error list was surfaced before the file-lock/temp-file issues

## Likely Next Work
- Test quotation to invoice conversion end to end after running SQL script
- If needed, refine invoice draft flow created from quotation
- Continue next business module only after conversion flow is verified

## Good Resume Prompt For Next Session
```text
Read docs/SESSION_NOTES.md, inspect the current BizCore codebase, and continue from the latest unfinished work.
```
