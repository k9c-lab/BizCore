# Task: Workflow Standardization

## Objective
Standardize action buttons, status flow, validation, and disabled-button explanation across Quotation, Invoice, Payment, and Receipt.

## Requirements

1. Quotation
- Approve by action button
- Convert to Invoice by action button
- Disable when not allowed and show reason

2. Invoice
- Issue Invoice by action button
- Cancel Invoice by action button
- Receive Payment by action button
- Disable when not allowed and show reason

3. Payment
- Post Payment by action button
- Generate Receipt by action button
- Cancel Payment by action button
- Disable when not allowed and show reason

4. Receipt
- Cancel Receipt by action button
- Disable when not allowed and show reason

5. Remove dependence on editing status field manually where still used

## Constraints
- Keep existing business logic
- Standardize UI/UX and validation behavior