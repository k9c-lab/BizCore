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

## Latest Session Update - 2026-04-20
- User login MVP is implemented and tested by user.
  - Cookie authentication is enabled globally.
  - Login/logout pages exist.
  - Default seeded login password is `Admin@12345`.
- Role/module permissions were added:
  - `Admin`: all modules
  - `Sales`: Quotations, Invoices, Payments, Receipts
  - `Warehouse`: Purchase Orders, Receivings, Stock Inquiry, Serial Inquiry, Supplier Claims
  - `Viewer`: dashboard/basic authenticated access only
- Sidebar was reorganized by module and now hides modules the current role cannot access.
- Sidebar active state was added.
- Footer/layout overlap issue was fixed earlier; action buttons should remain clickable.
- Workflow action messages were changed from red validation style to neutral `workflow-info` style where applicable.
- Purchase Order pages were standardized:
  - searchable supplier/item UI
  - action buttons for workflow
  - PO print page
  - PO audit/history at bottom of details page
- Receiving pages were standardized:
  - PO read-only information and summary
  - clearer receiving line layout
  - serial input split into individual textboxes
  - per-line readiness guidance
  - Save Draft and Post Receiving behavior refined
  - Cancel Receiving implemented
  - Receiving print page
  - Receiving audit/history at bottom of details page
- Stock Inquiry serial list was changed to a card-style layout similar to Serial Inquiry.
- Payment/Receipt layout was refactored to match Quotation/Invoice style.
- Receipt module was implemented:
  - `ReceiptHeaders`
  - Generate Receipt from posted payment
  - Receipt list/details/print
  - Duplicate receipt blocked for same payment
  - Cancel Receipt implemented without changing payment/invoice balances
- Payment cancel was implemented:
  - only posted payments can be cancelled
  - issued receipt blocks payment cancellation
  - cancellation reverses invoice allocations and recalculates invoice status
- Payment post validation now requires full allocation:
  - allocated amount must equal payment amount
  - over-allocation blocked
  - remaining amount shown
- Customer autocomplete/searchable dropdown was added across quotation/invoice/payment related pages.
- Audit/history fields were added to sales and finance modules:
  - Quotation: created, updated, approved, converted
  - Invoice: created, updated, issued, cancelled
  - Payment: created, posted, cancelled
  - Receipt: created, issued, cancelled
- Details pages now show compact `Document History` at the bottom for:
  - Purchase Orders
  - Receivings
  - Quotations
  - Invoices
  - Payments
  - Receipts

## Latest SQL Scripts To Run
- Existing database should have these scripts applied in order if not already run:
  - `database/016_user_login_mvp.sql`
  - `database/017_receiving_audit_fields.sql`
  - `database/018_purchase_order_audit_fields.sql`
  - `database/019_receiving_updated_by_audit.sql`
  - `database/020_sales_finance_audit_fields.sql`
- User ran `020` once and hit SQL Server compile error:
  - `Invalid column name 'CreatedByUserId'`
  - Cause: SQL Server parsed the `UPDATE` batch before recognizing columns added earlier in the same batch.
  - Fix already made: `020_sales_finance_audit_fields.sql` was rewritten with `GO` batch separators.
  - Next action: reopen the updated `020` file in SSMS and run it again.

## Latest Verification
- Build command used to avoid locked running `BizCore.exe`:
  - `dotnet build .\src\AccountingSystem\BizCore.csproj --no-restore -p:UseAppHost=false -p:OutputPath=$env:TEMP\BizCoreAuditBuild`
- Latest result:
  - Build succeeded
  - 0 warnings
  - 0 errors
- A normal build may fail if the website is currently running and locking `bin\Debug\net8.0\BizCore.exe`; this is a file-lock issue, not a compile issue.

## Suggested Next Steps
- Run the updated `database/020_sales_finance_audit_fields.sql`.
- Restart the website after database update.
- Quick role test:
  - `admin` should see all modules.
  - `sales.manager` should see Sales module only.
  - `inventory.staff` should see Purchasing/Inventory/Warranty modules only.
- Quick audit test:
  - create a new quotation/invoice/payment/receipt or PO/receiving
  - open Details
  - check `Document History` at the bottom.
- Before pushing to GitHub:
  - review `git status`
  - avoid committing runtime logs and temp build artifacts such as `bizcore-run*.log`, `bizcore-direct*.log`, and `obj_verify/`.

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
- Implemented invoice status control and cancel logic:
  - only `Draft` invoices can be edited
  - `Issued` and `Cancelled` invoices are read-only
  - added Cancel Invoice action
  - cancel is blocked if invoice has payment/allocation
  - cancelling issued invoice restores stock
  - cancelling issued invoice restores serials to `InStock`
  - cancelling issued invoice clears customer assignment and customer warranty fields
  - cancel logic uses database transaction
- Fixed quotation-to-invoice draft serial flow:
  - quotation conversion creates invoice as `Draft`
  - conversion copies quotation lines only
  - conversion does not assign serials
  - conversion does not deduct stock
  - serials are selected later in invoice draft/edit stage
- Added invoice issue flow for draft invoices:
  - draft invoice can be saved without complete serial selection
  - issuing invoice requires serial count to equal quantity
  - selected serial count cannot exceed quantity
  - issue deducts stock and assigns serial/customer/customer warranty
  - issue sets invoice status to `Issued`
- Improved invoice issue validation UX:
  - `Invoices/Details` disables Issue Invoice when required serials/warranty are incomplete
  - page shows clear reason instead of silently redirecting to edit
  - backend validates again before issue
- Improved quotation approval UX:
  - quotation status is no longer edited by changing the status field and saving
  - added explicit Approve action for draft quotations
  - Convert to Invoice is enabled only when quotation is approved
  - Save Draft action remains for normal save
- Improved quotation item UX:
  - product items show CurrentStock hint after item selection
  - service items do not show stock hint

## Scripts To Run On Existing Database
- If not already applied, run:
  - `database/014_quotation_to_invoice_reference_update.sql`
- Latest invoice status/cancel and UX work did not add a new database script

## Build / Verification Notes
- After invoice status/cancel and draft issue changes, build succeeded once with:
  - `dotnet build BizCore.csproj --no-restore -p:UseAppHost=false`
  - result: 0 errors, 1 warning because running app locked `BizCore.exe`
- After latest quotation/invoice UX refinements, build reached the copy step but failed because the running app locked `BizCore.dll`
- Stop the running website/BizCore process before the next full build
- No compiler or Razor errors were shown before the file-lock failure

## Likely Next Work
- Stop running website, then run full build
- Test quotation approval and Convert to Invoice end to end
- Test draft invoice edit:
  - serial selection for serial-controlled products
  - selected serial count cannot exceed quantity
  - issue blocked until serials/warranty are complete
- Test invoice cancel:
  - cannot cancel paid invoice
  - issued invoice cancellation restores stock and serials
- Optional future UX improvement:
  - make `Invoices/Edit` layout fully match the cleaner form-first `Invoices/Create` experience

## Good Resume Prompt For Next Session
```text
Read docs/SESSION_NOTES.md, inspect the current BizCore codebase, and continue from the latest unfinished work.
```
