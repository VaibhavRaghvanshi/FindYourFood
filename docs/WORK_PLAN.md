# Work plan

1. Completed the Windows audit and first Identity, Gateway and Angular registration slice.
2. Completed local owner bootstrap and staff MFA. Production OIDC, email verification, session lifecycle, recovery, security audit and key rotation remain.
3. Completed the local scoped Organization delegation, revocation and admin slice. Departments, staff invitations and later service policy enforcement remain.
4. Completed the local customer homepage Experience slice, Provider onboarding with staff review and branch operations, Catalog draft and published branch menus, and Customer profiles and delivery addresses. Provider personnel and production document handling remain. Expand Experience with managed assets, city and role variants, campaign targeting and real restaurant collections as those services become available.
5. Add Discovery and Location with customer search, manual address and nearby ranking.
6. Add Cart and Ordering with idempotent checkout, immutable snapshots and a durable saga.
7. Add the transactional outbox and RabbitMQ profile. Then add Payment, Delivery and Finance in separate vertical slices.
8. Add Campaign and Notification, Kafka projections, Support, owner dashboards and reporting.
9. Validate reliability, scale, security and production deployment separately.

The full ordered roadmap and acceptance criteria are preserved in [Product requirements](requirements/PRODUCT_REQUIREMENTS.md).
