# Service communication

Use authenticated REST with typed clients, cancellation and bounded timeouts for immediate decisions such as checkout pricing, serviceability and current permissions. Retries require idempotent operations and a bounded budget. Keep dependency chains short and avoid using the public gateway for east-west calls.

Use RabbitMQ for operational commands and Kafka for replayable analytics or projection streams. Producers write an outbox row with the same transaction as the domain change. Workers publish with confirmation and retry on ambiguity. Consumers deduplicate by message ID and update business data and inbox together before acknowledging. Version schemas and track poison messages for reviewed replay.

## Ordering saga

Ordering durably creates a pending order under a customer-scoped idempotency key. It checks authoritative menu price, provider availability and Location serviceability, and records an immutable quote and order snapshot. Online payment is authorized or reconciled against the provider. COD proceeds only when eligible. The provider accepts within a recorded deadline. Capture after acceptance, confirm the order, request dispatch, track delivery and emit the settlement event after verified delivery.

Before capture, rejection or expiry voids the authorization. After capture, an eligible cancellation initiates a refund using an idempotency key. On ambiguous payment results, reconcile before retry. If assignment fails, retry within the promised window, then escalate for reassignment or cancellation under the agreed policy. Each step records state, deadline, retry count and idempotency key, with compensations persisted before execution. Exhausted retries move to manual review with an audit trail. Exact timeouts and settlement rules are decided in the ordering and payment milestones.

## Caching

Cache only suitable derived reads. Use a Redis-compatible Valkey adapter for discovery or published configuration when needed. Bound key cardinality and TTL, version keys, coalesce refreshes and invalidate on events. During cache failure, use bounded source reads or a safe public fallback. Keep payments, orders, authorization decisions requiring freshness and final dispatch assignments in their authoritative stores.
