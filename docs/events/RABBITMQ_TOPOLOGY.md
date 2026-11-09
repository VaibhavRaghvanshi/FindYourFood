# RabbitMQ topology

Introduce a durable operational exchange in the messaging milestone. Use versioned keys such as `delivery.assign.v1` and `notification.send.v1`, one durable queue per consumer capability, explicit acknowledgements, controlled prefetch and bounded retry queues. Dead-letter exhausted messages with diagnostics for reviewed replay. Confirm producer delivery before marking outbox rows sent. Keep application state and inbox deduplication in SQL. Set separate service credentials and least-privilege queue ACLs.

Windows installation requires a supported Erlang/RabbitMQ pair. The Identity profile runs without a broker.
