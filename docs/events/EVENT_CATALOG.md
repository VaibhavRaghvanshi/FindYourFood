# Event catalog

Planned envelope fields include `messageId`, `type`, `schemaVersion`, `occurredAt` (UTC), `producer`, `aggregateId`, `correlationId`, optional `causationId` and a validated payload. A command has an imperative name and one owning handler. An event records an already committed fact. Track schema compatibility and document privacy constraints before publishing.

| Event | Producer | Candidate consumers |
| --- | --- | --- |
| CustomerRegistered | Identity | Customer |
| PermissionChanged | Organization | Policy cache listeners |
| ProviderApproved / RestaurantAvailabilityChanged | Provider | Discovery |
| MenuPublished | Catalog | Discovery |
| OrderPlaced / OrderAccepted / OrderCancelled | Ordering | Notification and analytics |
| PaymentCaptured / PaymentFailed / RefundCompleted | Payment | Ordering and Finance |
| RiderAssigned / FoodPickedUp / OrderDelivered | Delivery | Ordering, Notification and Finance |
| ExperiencePublished / CampaignPublished | Experience / Campaign | Configuration cache listeners |

These contracts enter runtime with their workflows. Registration currently writes the Identity account only.
