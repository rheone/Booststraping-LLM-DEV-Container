# Reverse Engineer — Examples

## overview.md snippet

```md
# Project Overview

_Sources: src/main.ts, src/app.module.ts, src/api/router.ts, package.json_

## Summary
A REST API for a B2B e-commerce platform. Manages product catalogs, customer orders, and inventory allocation across multiple warehouses. Integrates with an external payment processor and a third-party logistics provider for shipment tracking.

## Technology stack
| Layer | Technology |
|-------|-----------|
| Runtime | Node.js 20, TypeScript |
| Framework | NestJS |
| Database | PostgreSQL via TypeORM |
| Queue | BullMQ (Redis) |
| Auth | JWT + role-based access via custom guard |

## Entry points
- `POST /api/v1/orders` — place a new order
- `GET /api/v1/orders/:id` — retrieve order status
- `POST /api/v1/catalog/products` — create product listing (admin)
- `POST /webhooks/payment` — receive payment processor callbacks

## Domain map
| Domain | Responsibility |
|--------|---------------|
| Orders | Order lifecycle from placement through fulfillment |
| Inventory | Stock tracking and warehouse allocation |
| Catalog | Product and pricing management |
| Payments | Payment initiation, capture, and refund |
| Shipping | Carrier integration and shipment tracking |

## Key data flows
1. Customer places order → inventory reserved → payment authorized → order confirmed
2. Warehouse ships → ShipmentConfirmed event → payment captured → order fulfilled
3. Customer cancels → inventory hold released → payment authorization voided

Confidence: High
```

---

## Domain doc snippet

```md
# Orders Domain

_Sources: src/orders/OrderService.ts, src/orders/OrderRepository.ts, src/orders/dto/, tests/orders/_

## Responsibility
Owns the full lifecycle of a customer order: creation, validation, payment authorization, fulfillment handoff, and cancellation. Enforces business rules around order limits and inventory holds. Acts as the saga orchestrator between Inventory and Payments.

## Key entities
| Entity | Role |
|--------|------|
| Order | Root aggregate; tracks status and line items |
| OrderLine | Individual SKU, quantity, and price snapshot at order time |
| FulfillmentRequest | Projection emitted to the Shipping domain on order confirmation |

## Operations
| Operation | Trigger | Effect |
|-----------|---------|--------|
| PlaceOrder | POST /orders | Creates Order, reserves inventory, initiates payment auth |
| CancelOrder | POST /orders/:id/cancel | Releases inventory hold, voids auth if payment uncaptured |
| ConfirmFulfillment | ShipmentConfirmed event | Captures payment, sets Order status to FULFILLED |

## Integrations
- **Inventory** — synchronous stock reservation on order placement; release on cancellation
- **Payments** — async auth/capture via PaymentsService queue
- **Shipping** — publishes FulfillmentRequest event; consumes ShipmentConfirmed event

Confidence: Medium — event saga flow inferred from handler names and BullMQ queue definitions; no integration tests cover the full saga path.

## Features
- [place-order](features/place-order.md) — accept cart payload, reserve inventory, initiate payment auth
- [cancel-order](features/cancel-order.md) — release holds and void authorization
- [confirm-fulfillment](features/confirm-fulfillment.md) — capture payment on shipment confirmation
```

---

## Feature doc snippet

```md
# Place Order

_Sources: src/orders/OrderService.ts:87–134, src/orders/dto/CreateOrderDto.ts, tests/orders/OrderService.test.ts_

## What it does
Accepts a cart payload from an authenticated customer, validates line items against current catalog prices, reserves inventory for each SKU, and initiates a payment authorization. Returns the created Order ID with status PENDING_PAYMENT.

## Entry point
`POST /api/v1/orders` — requires authenticated customer session (JWT bearer token)

## Steps
1. Validate `CreateOrderDto` — reject if any SKU is missing, quantity ≤ 0, or required fields absent.
2. Fetch current prices from `CatalogService` for each line item to snapshot at order time.
3. Check order total does not exceed $50,000 — reject with HTTP 422 if exceeded.
4. Call `InventoryService.reserve()` for all line items atomically — rolls back all holds on partial failure.
5. Enqueue `PlaceOrderCommand` to the orders BullMQ queue for async payment authorization.
6. Return `{ orderId, status: "PENDING_PAYMENT" }`.

_Source: tests/orders/OrderService.test.ts:142 — "should reject orders exceeding $50,000 limit"_
_Source: tests/orders/OrderService.test.ts:201 — "should return existing order for duplicate idempotency key"_

## Authorization
- Authorization is handled authorization service and allows users with the roles Admin, ReadOnly, ApplicationTestes

## Validation
- Controller based validation validates that the user may access the endpoint
- Dto validators include DateValidator, KnownStringValidator, and GreaterThanOneValidator

## Outputs / side effects
- Creates `Order` record with status `PENDING_PAYMENT`
- Creates `InventoryHold` records per line item SKU
- Enqueues `PlaceOrderCommand` for async payment processing

## Edge cases and constraints
- Idempotency: `X-Idempotency-Key` header required; duplicate key returns the existing order unchanged.
- Partial inventory failure: if any SKU reservation fails, all holds for this order are released.

Confidence: High
```

---

## open-questions.md entries

```md
| # | Domain | Question | Confidence | Evidence |
|---|--------|---------|-----------|---------|
| 1 | Orders | Does CancelOrder release the inventory hold synchronously or via a compensating event? The handler calls InventoryService but it is not clear whether this is a direct call or queued. | Low | No tests cover cancellation after fulfillment handoff has started. |
| 2 | Payments | What is the retry policy for failed payment webhook deliveries? The webhook handler exists but no retry configuration or dead-letter queue is visible in the codebase. | Low | Confidence: Low — webhook processor at src/payments/WebhookProcessor.ts has no retry decorator and no corresponding test. |
```

---

## confidence-summary.md

```md
# Confidence Summary

## Roll-up by domain

| Domain | High | Medium | Low | Notes |
|--------|------|--------|-----|-------|
| Orders | 8 | 2 | 1 | Event saga inferred from queue names; no saga integration tests |
| Inventory | 5 | 4 | 2 | Allocation priority algorithm inferred from field names |
| Payments | 3 | 1 | 3 | External webhook handling largely undocumented in tests |
| Shipping | 2 | 3 | 1 | Carrier-specific logic not unit-tested |
| Catalog | 7 | 1 | 0 | Well-tested; pricing rules explicit in service layer |

## Items requiring human review
- [Payments: webhook retry behavior](../domains/payments/features/webhook-processing.md) — Confidence: Low — no retry configuration found
- [Orders: cancel-after-fulfillment behavior](../domains/orders/features/cancel-order.md) — Confidence: Low — no tests cover this path
- [Inventory: allocation priority](../domains/inventory/features/reserve-stock.md) — Confidence: Low — inferred from field ordering

## Coverage gaps
- Worker processes in `src/workers/` not yet documented
- Admin API routes under `/api/admin/` excluded from this run
- Background job schedules in `src/scheduler/` not analyzed
```

---

## run-log.md after batch discovery

```md
# Run Log

## Last run
- Date: 2025-11-14
- Mode: batch
- Domains discovered: 5
- Features discovered: 18

## Discovery plan

### Domains
- [x] orders — full order lifecycle and saga orchestration
- [x] inventory — stock and warehouse allocation
- [x] catalog — product and pricing management
- [x] payments — payment auth, capture, and webhook handling
- [x] shipping — carrier integration and tracking

### Features per domain
- [x] orders/place-order
- [x] orders/cancel-order
- [x] orders/confirm-fulfillment
- [x] inventory/reserve-stock
- [x] inventory/release-hold
- ...

## Generation status
| Document | Status | Notes |
|----------|--------|-------|
| overview.md | Complete | |
| domains/orders.md | Complete | |
| domains/orders/features/place-order.md | Complete | |
| domains/payments.md | Complete | 3 Low-confidence items → open-questions |
| _meta/glossary.md | Complete | |
| _meta/open-questions.md | Complete | 5 items |
| _meta/confidence-summary.md | Complete | |
```
