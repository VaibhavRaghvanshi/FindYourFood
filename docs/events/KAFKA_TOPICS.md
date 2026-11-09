# Kafka topics

Introduce a dedicated KRaft integration profile for replayable streams. Proposed topics include `fyf.order.lifecycle.v1` keyed by order ID, `fyf.discovery.catalog.v1` keyed by provider/branch and `fyf.operations.delivery.v1` keyed by delivery ID. Partitioning gives per-key ordering. Give each independent projection a consumer group and use durable inbox deduplication. Set retention based on reconstruction requirements and customer data policies. Monitor lag and test replay in a separate group before enabling recovery workflows.

Publish only contracts with a replay use case. Operational commands remain on RabbitMQ. Identity has no Kafka runtime dependency.
