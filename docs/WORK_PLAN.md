# Work plan

1. Completed the Windows audit and first Identity, Gateway and Angular registration slice.
2. Completed local owner bootstrap and staff MFA. Production OIDC, email verification, session lifecycle, recovery, security audit and key rotation remain.
3. Completed the local scoped Organization delegation, revocation and admin slice. Departments, staff invitations and later service policy enforcement remain.
4. Completed the local customer homepage Experience slice, Provider onboarding with staff review and branch operations, Catalog draft and published branch menus, and Customer profiles and delivery addresses. Provider personnel and production document handling remain. Expand Experience with managed assets, city and role variants, campaign targeting and real restaurant collections as those services become available.
5. Implemented local Discovery and Location with approved published menu search, opening hours, manual coordinates, delivery coverage and nearby ranking. Production indexing, geocoding, road/traffic adapters, scoped zone delegation and preference ranking remain.
6. Implemented local Cart and Ordering with customer and guest ownership, fresh price validation, delivery quotes, scoped checkout idempotency, immutable order snapshots, private history and cancellation. The initial durable saga expires unpaid orders. Tax policies and campaign pricing remain.
7. Ordering records pending events in its own transactional outbox. Add reliable outbox publication, inbox processing and the native RabbitMQ profile. Then add Payment, Delivery and Finance in separate vertical slices.
8. Add Campaign and Notification, Kafka projections, Support, owner dashboards and reporting.
9. Validate reliability, scale, security and production deployment separately.

The full ordered roadmap and acceptance criteria are preserved in [Product requirements](requirements/PRODUCT_REQUIREMENTS.md).
