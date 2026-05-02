# Neura Training Checklist - 2026-05-04

## Purpose
- Quick pre-training checklist for the first live customer deployment: `neura`.
- Focus on verifying that the current production flow still works as expected.
- Current pricing expectation:
  - `Sales.PricingMode` should remain `SinglePrice`
  - selling flow should continue using `Items.UnitPrice`

## Before Training
- Confirm latest code deployment completed successfully.
- Log in with an `Admin` account.
- Open `Settings`.
- Confirm `Pricing Mode = SinglePrice`.
- Open `Settings -> Database Upgrade`.
- Confirm there are no unexpected pending scripts before training.
- If pending scripts are expected and approved:
  - backup database first
  - run pending migrations
  - refresh and confirm upgrade completed

## Core Access Check
- Login page works.
- Admin login works.
- Welcome page loads.
- Sidebar menus load correctly for admin.
- Users page opens.
- Items page opens.
- Quotations page opens.
- Invoices page opens.
- Purchase Orders page opens.
- Receivings page opens.

## Item Master Check
- Open `Master Data -> Items`.
- Open an existing item.
- Confirm `UnitPrice` is visible and unchanged.
- Confirm item edit page saves normally.
- Confirm no multi-price UI is shown yet in the current customer flow.

## Quotation Check
- Open `Sales -> Quotations`.
- Create a new quotation.
- Select customer successfully.
- Add at least one item successfully.
- Confirm line `Unit Price` is filled from the item master as before.
- Confirm totals still calculate normally.
- Save draft or save quotation successfully.

## Invoice Check
- Open `Sales -> Invoices`.
- Create a new invoice.
- Select customer successfully.
- Add at least one item successfully.
- Confirm `Unit Price` still behaves as before.
- Confirm stock display still appears for stock items.
- Save draft successfully.
- If safe for test data, issue one test invoice and confirm success.

## Purchasing Check
- Open `Purchasing -> Purchase Orders`.
- Confirm PO list loads.
- Open one PO or create a test PO if needed.
- Confirm item selection still works.
- Confirm PO `Unit Price` behavior is unchanged.

## Receiving Check
- Open `Purchasing -> Receivings`.
- Confirm receiving list loads.
- Open a receiving document or create a test draft if needed.
- Confirm form loads without error.

## Optional Admin Check
- Open `Settings`.
- Confirm page loads without error.
- Confirm `Database Upgrade` section shows applied/pending script state.
- Do not switch to `MultiPrice` before training unless there is a deliberate test plan.

## If Anything Fails
- Stop and capture:
  - page name
  - action attempted
  - exact error message
- Re-check:
  - latest deployment files
  - pending migration status
  - whether the instance was upgraded after deployment

## Training Message
- Current live workflow remains on the existing single-price model.
- Multi-price is only foundation work at this stage and is not active in the operational sales flow yet.
