# SaaS / Product Architecture Notes

## Purpose
- Keep long-term product design principles in one place.
- Use this document for decisions that should survive across sessions, customers, and feature rounds.
- Do not use this file for daily progress logs; use `SESSION_NOTES.md` for that.

## Current Deployment Model
- Product direction is SaaS-minded, but deployment is currently `single-tenant per customer`.
- Each customer deployment has:
  - separate database
  - separate web/app deployment
  - separate configuration and release timing
- Current design implication:
  - do not force `TenantId` into every table right now
  - prefer per-instance configuration over customer-specific code forks
  - prefer per-instance upgrade execution through an admin-safe SQL migration runner

## Core Product Principles
- Keep one shared codebase for all customers whenever possible.
- New features should be configurable per deployment.
- Core workflows should stay simple for customers using only baseline features.
- Advanced workflows should be opt-in, not forced on every customer.
- Avoid schema or UI decisions that assume one customer's business model is universal.
- Prefer generic naming over customer-specific terminology.

## Feature Design Rules
- Design features to support both:
  - `baseline mode`
  - `advanced mode`
- Use configuration / settings / feature flags where behavior may differ between customers.
- Preserve fallback behavior so existing customers are not broken by new capability.
- Avoid hardcoding optional business rules into the core flow when they can be configuration-driven.

## Pricing Direction
- Pricing should be designed with SaaS flexibility in mind.
- Not every customer will use multi-price selling.
- Product should support:
  - `SinglePrice` mode
  - `MultiPrice` mode
- Current implementation foundation:
  - admin-configurable `Sales.PricingMode` stored in `SystemSettings`
  - safe default stays `SinglePrice` so existing customers remain on the legacy flow
- `Items.UnitPrice` should remain usable as base/default/fallback price.
- If multi-price is added later, prefer flexible structures such as:
  - `PriceLevels`
  - `ItemPrices`
  - optional customer default price level
- Avoid locking the product to fixed columns like `RetailPrice`, `WholesalePrice`, `OnlinePrice` unless there is a very strong reason.

## Costing Direction
- Real costing should be designed as a product capability, not a one-customer shortcut.
- Not every customer will need advanced costing immediately.
- Prefer phased evolution:
  - simple/base costing if needed
  - stronger stock-linked costing when product is ready
- When costing is implemented, design it so reporting, stock, and sales posting can evolve without rewriting the whole model.

## When Adding New Features
- Ask:
  - Is this needed by all customers or only some?
  - Should this be configuration-driven?
  - Is the default experience still simple for smaller customers?
  - Will this create a customer-specific fork if implemented naively?
  - Is there a safe fallback for older deployments?

## Documentation Rule
- Put permanent design principles in this file.
- Put execution history, build results, and change logs in `SESSION_NOTES.md`.

## Database Upgrade Direction
- Default direction is `SQL-based migration system`, not EF Core migrations.
- Use safe, ordered SQL files for upgrades after customers are already live.
- Migration scope should include:
  - schema changes
  - system-owned setup data
  - product configuration defaults
- Migration scope should exclude:
  - demo data
  - customer-owned business data
  - cleanup/reset/handover scripts
- Preferred execution model:
  - admin-visible upgrade screen
  - pending/applied migration visibility
  - controlled `Run Pending Migrations` action per deployed instance
