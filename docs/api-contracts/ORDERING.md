# Ordering API

Ordering listens on 5110 and owns `FindYourFood_OrderingDb`. It validates checkout through Cart's HTTP quote contract.

| Method | Path | Behavior |
| --- | --- | --- |
| POST | `/api/v1/orders` | Create an order with a scoped idempotency key and confirmed total. |
| GET | `/api/v1/orders?page=1` | Read 20 private order summaries for the authenticated subject or guest capability. |
| GET | `/api/v1/orders/{id}` | Read the immutable snapshot and current order status. |
| POST | `/api/v1/orders/{id}/cancel` | Cancel an order awaiting payment or restaurant acceptance. |

A checkout request contains idempotencyKey, cartId, expectedCartRevision, expectedTotalMinor and delivery. Delivery contains name, phone, street, city, postalCode, latitude and longitude. Phone accepts an optional plus sign and 10–15 digits. The delivery point must be quoted through Cart.

Ordering obtains a fresh Cart quote, validates line totals, the menu version, cart expiry and delivery point, and compares the expected total. Price or availability changes return 409 before creating a record. An upstream outage returns 503 and the same request can be retried.

SQL enforces unique owner/idempotency-key and cart/revision pairs. A repeated request with the same normalized checkout returns the existing order even after the cart has been discarded. A changed request for an existing checkout returns 409 with an existingOrderId only when the caller owns that order.

Customer ownership comes from the verified token subject. Guest checkout forwards `X-Cart-Key` to Cart and stores its SHA-256 hash for private order access. Raw guest capabilities and bearer tokens are excluded from snapshots and event payloads. Privileged staff accounts cannot use customer checkout routes. Responses use no-store.

An order, immutable item/address/price snapshot, checkout saga and two pending outbox messages commit in one SQL transaction. New orders enter AwaitingPayment. The payment deadline defaults to 15 minutes and is configurable from 1 to 60 minutes with `Ordering:PaymentTimeoutMinutes`. A background worker and private status reads expire unpaid orders. Cancellation and its events also commit atomically.

The state machine defines payment, acceptance, preparation, pickup, delivery and cancellation stages. The initial slice exposes customer creation, history, status and cancellation. Payment and delivery integrations will drive subsequent stages. Outbox publication and inbox processing belong to the messaging profile.

The Angular customer app provides `/checkout`, `/orders` and `/orders/{id}`. Guest access remains in tab session storage after checkout. Order status polls every five seconds while the page is visible and stops on terminal stages.
