# Task: Receipt Module

## Objective
Implement receipt generation and print from payment.

## Requirements

1. Create ReceiptHeaders table

2. From Payment page:
- Add button "Generate Receipt"
- Only allow if Payment is Posted

3. On generate:
- Create ReceiptHeader
- Link to PaymentId

4. Receipt page:
- Show customer
- Show payment amount
- Show invoice allocations

5. Receipt print:
- A4 layout
- Clean document style
- Similar to Quotation/Invoice

## Constraints
- Do not modify payment logic
- Do not implement receipt cancellation reversal yet