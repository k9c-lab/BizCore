# Customer Autocomplete Specification

## Objective
Standardize all customer selection fields across the system using searchable dropdown / autocomplete.

---

## Scope
Apply to all pages that select Customer, including:
- Quotations/Create
- Quotations/Edit
- Invoices/Create
- Invoices/Edit
- Payments/Create
- Receipts pages where customer selection is needed
- Any other page using Customer dropdown

---

## Requirements

1. Replace normal Customer dropdown with searchable autocomplete dropdown

2. Search fields should support:
- CustomerCode
- CustomerName
- TaxId

3. After selecting customer, show read-only customer information in consistent format:
- CustomerName
- TaxId
- ContactName
- Phone
- Email
- BillingAddress

4. UI must be consistent across all modules

5. Keep existing customer business logic unchanged