# Change Guardrails

## Purpose

This project has reached a more stable stage. Future changes should protect existing workflows and avoid unintended regressions in daily operations.

## Core Rule

Before making any non-trivial change, review likely workflow impact first. If the change may affect business behavior, align with the customer before implementation.

## Impact Check Areas

Always consider whether a change can affect:

- `Create`
- `Edit`
- `Details`
- `Print`
- `Report`
- `Permission`
- calculations
- posting-related behavior
- data entry speed and usability

## Changes That Require Alignment First

Pause and summarize impact before implementation when a change touches:

- tax / VAT logic
- totals, discounts, or amount calculations
- stock movement or stock allocation
- document workflow
- permissions
- validation rules
- print layout or printed totals
- searchable dropdown behavior or other data-entry interaction patterns
- database schema that can alter workflow or user behavior

## Low-Risk Changes

The following can usually proceed directly, while still being called out clearly as low-risk UI work:

- label wording
- spacing and layout polish
- icon changes
- non-behavioral text updates

## Expected Working Style

For changes with possible workflow impact, summarize:

1. what will change
2. what workflow or business areas may be affected
3. what risks should be watched
4. what should be confirmed before implementation

## Goal

Keep the system stable while still allowing steady improvement, with explicit impact awareness before edits are made.
