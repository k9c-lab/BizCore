# Task: Simple Supplier Claim

## Objective
Simplify existing claim screen into serial-based supplier claim log.

## Requirements
1. Remove claim header/detail workflow
2. Create table:
   - SerialClaimLogs
3. Use existing claim page and simplify it
4. Claim is created directly from selected serial
5. Claim page must not be a search page
6. Validate:
   - ClaimDate must be within supplier warranty
7. On save:
   - insert SerialClaimLogs
   - update SerialNumbers.Status = ClaimedToSupplier

## Constraints
- Do not implement full claim document workflow
- Do not implement replacement stock
- Do not implement receive-back workflow yet