# FINDYOURFOOD — ULTIMATE MICROSERVICES MASTER PROMPT
## Version 2.0 — Complete Backend, Frontend, Infrastructure, Security, Delivery, Dynamic UI and DevOps

**PROJECT:** FindYourFood  
**ARCHITECTURE:** Independent Microservices + Database per Service + Clean Architecture  
**BACKEND:** ASP.NET Core .NET 10 LTS  
**FRONTEND:** Angular 22 Stable  
**DATABASE:** Microsoft SQL Server  
**API GATEWAY:** YARP  
**EVENT MESSAGING:** RabbitMQ + Apache Kafka  
**DISTRIBUTED CACHE:** Redis-compatible Valkey or appropriately licensed Redis  
**LOCAL DEVELOPMENT:** Windows, without Docker  
**DEVELOPMENT APPROACH:** Small, tested, end-to-end vertical slices  
**SOURCE CONTROL:** Git and GitHub

---

# SECTION 1 — YOUR RESPONSIBILITY AS CODEX

Act as a Principal Software Architect, Senior ASP.NET Core Engineer, Angular Architect, Distributed Systems Engineer, Database Architect, DevOps Engineer, Security Engineer, SRE and QA Lead.

You are responsible for developing a complete commercial-grade food delivery platform called **FindYourFood**.

The human user is the owner and final decision-maker.

Your objectives are:

1. Design a true distributed microservices architecture.
2. Implement every business microservice independently using Clean Architecture.
3. Give every microservice its own private database.
4. Build complete Angular applications for customers, providers, riders and administrators.
5. Implement secure identity, organizational hierarchy and fine-grained access control.
6. Implement restaurant discovery, intelligent dispatch, routing and delivery tracking.
7. Implement backend-controlled themes, layouts and festival campaigns.
8. Introduce appropriate messaging, caching, observability and resilience technologies.
9. Use free and maintainable developer technologies wherever possible.
10. Run locally without Docker.
11. Build and test each backend/frontend feature together.
12. Commit tested progress and push through authorized GitHub workflows.
13. Preserve requirements, architecture decisions and work progress across Codex sessions.

**Do not replace the microservices architecture with a monolith or modular monolith.**

A separate ASP.NET Core deployment, code boundary and private database must exist for each chosen business microservice.

A microservice is a bounded business capability, not an individual controller or endpoint.

Do not create one independently deployed service for every HTTP endpoint.

Optimize boundaries for business autonomy, correctness and realistic operating costs.

---

# SECTION 2 — ABSOLUTE DEVELOPMENT RULES

## 2.1 Microservices are mandatory

Every business microservice must have:

- Its own independently runnable ASP.NET Core host.
- Its own Domain layer.
- Its own Application layer.
- Its own Infrastructure layer.
- Its own API/Presentation layer.
- Its own private database.
- Its own EF Core migrations where persistence is required.
- Its own configuration.
- Its own unit/integration test projects.
- Its own health and readiness checks.
- Its own logging and telemetry.
- Its own deployment configuration.
- A clearly defined API/event contract.

Sharing one SQL Server instance for local development is permitted.

Sharing the same database between services is not permitted.

## 2.2 Do not use Docker

Do not install, configure or require:

- Docker Desktop.
- Docker Compose.
- Kubernetes.
- Minikube.
- Docker-based Testcontainers.
- Container-dependent local services.

The development machine has limited resources.

All required local applications must run using native Windows executables, services, .NET projects or supported lightweight development processes.

Docker can be introduced in a later approved infrastructure phase.

Do not stop application development merely because Docker is unavailable.

## 2.3 Avoid unnecessary resource consumption

Do not require all microservices to run together during routine development.

Provide development launch profiles such as:

- Identity and administration.
- Restaurant and catalog.
- Customer discovery.
- Ordering and payment.
- Dispatch and delivery.
- Full integration environment.

Start only the services needed for the current test.

Stop unused processes.

Use distinct ports, predictable configurations and lightweight startup scripts.

The project must support genuine independent services without forcing the developer to run the entire ecosystem continuously.

## 2.4 Free development first

Use maintained, free/open-source developer technologies wherever practical.

Before adding a dependency, verify:

- Actual licensing conditions.
- Supported .NET/Angular versions.
- Maintenance activity.
- Windows compatibility.
- Security risks.
- Runtime and memory requirements.
- Whether it meaningfully improves our application.

Do not purchase services, enable billing or create paid cloud resources without owner approval.

Where commercial infrastructure is unavoidable, use a clearly marked local development adapter.

## 2.5 High-quality code

Follow SOLID and Clean Architecture principles.

Avoid:

- God controllers.
- God services.
- Cross-service shared database access.
- Business logic in controllers.
- Hardcoded passwords.
- Hardcoded external service credentials.
- Unbounded SQL queries.
- Excessive synchronous network calls.
- Direct external provider calls scattered across business logic.
- Unnecessary libraries.
- Unexplained code duplication.
- Empty boilerplate projects with no business responsibility.

Use meaningful names for every folder, class, interface, file, DTO and method.

Prefer clean, testable code over code-generation volume.

---

# SECTION 3 — CORE TECHNOLOGY STACK

Use stable versions, checking compatibility with official documentation before installation.

## Backend

- ASP.NET Core .NET 10 LTS.
- C#.
- EF Core 10.
- SQL Server.
- ASP.NET Core Identity where appropriate.
- JWT/OIDC-compliant authentication.
- Policy-based authorization.
- API versioning.
- OpenAPI.
- Dependency Injection.
- HttpClientFactory.
- ASP.NET Core Health Checks.
- SignalR.
- .NET resilience pipelines / Polly.
- FluentValidation or equivalent maintained validation mechanism.
- MediatR only where it adds enough value to justify its licence and dependency.
- AutoMapper/Mapster only if needed; manual projections are acceptable and often preferable.

Do not assume any third-party package is free for all commercial use. Verify licences before adoption.

## API Gateway

- YARP Reverse Proxy.
- ASP.NET Core Authentication/Authorization.
- Gateway-level rate limiting.
- Route configuration.
- Proxy load balancing.
- Request correlation.
- Safe header forwarding.
- Gateway telemetry.

## Messaging

- RabbitMQ.
- Apache Kafka with modern KRaft architecture.
- Versioned integration contracts.
- Transactional outbox/inbox.
- Idempotent consumers.
- Dead-letter processing.
- Retry and replay policies.

## Caching

- Valkey or appropriately licensed Redis.
- Redis-compatible .NET client.
- In-process cache for appropriate noncritical data.
- Distributed cache abstraction.
- Cache invalidation and TTL policies.

## Observability

- OpenTelemetry.
- Serilog or suitable structured logging.
- Jaeger for distributed tracing.
- Prometheus for metrics where practical.
- Grafana OSS dashboards.
- Correlation and trace propagation.

## Frontend

- Angular 22.
- TypeScript.
- Standalone components.
- Signals and modern reactive patterns.
- Lazy loading.
- Typed reactive forms / supported modern forms APIs.
- Reusable design system.
- API client abstraction.
- Route guards.
- HTTP interceptors.
- Angular accessibility patterns.
- Optional SSR for public discovery pages when justified.

## Developer tooling

- Git.
- GitHub.
- ESLint.
- Prettier.
- EditorConfig.
- xUnit.
- Angular unit tests.
- Playwright.
- k6.
- PowerShell scripts.
- .NET Aspire optional, configured only for supported noncontainer project/executable resources.

Do not use unsupported or prerelease software automatically.

---

# SECTION 4 — HIGH-LEVEL DISTRIBUTED ARCHITECTURE

The target logical architecture is:

```text
CUSTOMER / PROVIDER / RIDER / ADMIN ANGULAR APPS
                       |
                       v
                 YARP API GATEWAY
                       |
       +---------------+---------------+
       |               |               |
       v               v               v
   Identity        Restaurants      Discovery
       |               |               |
   IdentityDB      RestaurantDB    DiscoveryDB
       |
       +---------- INTERNAL SERVICES ----------+
                                                |
      Organization   Catalog    Ordering       |
          |            |          |            |
       OrgDB        CatalogDB   OrderDB        |
                                                |
      Delivery      Payments    Notifications  |
          |            |          |            |
       DeliveryDB   PaymentDB   NotificationDB |
                                                |
      Campaigns      DynamicUI    Finance      |
          |             |           |          |
       CampaignDB      UiDB      FinanceDB     |
                                                |
       +-------------+-------------+-----------+
                     |
          RABBITMQ / KAFKA
                     |
            EVENT CONSUMERS
                     |
       REDIS / VALKEY CACHING
                     |
     OPENTELEMETRY / MONITORING
```

This is a conceptual representation.

Not every interaction should pass through Kafka or RabbitMQ.

Not every service needs Redis.

Not every operation needs a synchronous request.

Each module must use the simplest reliable communication pattern appropriate to its requirements.

---

# SECTION 5 — MICROSERVICE CATALOG

Use naming convention:

`FindYourFood.<ServiceName>.Api`

Each actual business microservice must own its persistence and domain model.

## 5.1 Identity Service

**API:** `FindYourFood.Identity.Api`  
**Database:** `FindYourFood_IdentityDb`

Responsibilities:

- Customer and staff authentication.
- Provider/rider account identities.
- Password management.
- OTP verification abstraction.
- Token issuance.
- Session management.
- Refresh-token handling.
- MFA.
- Account lockout.
- Security events.
- Account lifecycle.
- Authentication audit.

Do not duplicate Identity's user table in another service.

Other services reference stable user IDs.

## 5.2 Organization & Authorization Service

**API:** `FindYourFood.Organization.Api`  
**Database:** `FindYourFood_OrganizationDb`

Responsibilities:

- Owner/CEO/manager/employee organization structure.
- Departments.
- Staff positions.
- Reporting relationships.
- Delegated permissions.
- Role templates.
- Resource scopes.
- Permission revocation.
- Employee assignments.
- Access review.
- Authorization-policy management.

This is the source of truth for organizational permissions.

Identity authenticates who the user is.

Organization decides what authorized actions the user may perform.

## 5.3 Customer Service

**API:** `FindYourFood.Customer.Api`  
**Database:** `FindYourFood_CustomerDb`

Responsibilities:

- Customer profiles.
- Delivery addresses.
- Preferences.
- Favorites.
- Dietary settings.
- Guest customer records.
- Account-related privacy controls.
- Profile management.

Do not store authentication passwords in CustomerDB.

## 5.4 Food Provider Service

**API:** `FindYourFood.Provider.Api`  
**Database:** `FindYourFood_ProviderDb`

Responsibilities:

- Restaurant registration.
- Dhabas.
- Cloud kitchens.
- Cafes and bakeries.
- Home kitchens.
- Tiffin providers.
- Approval processes.
- Verification documents.
- Branches.
- Opening hours.
- Temporary closures.
- Provider personnel.
- Restaurant settings.

## 5.5 Catalog Service

**API:** `FindYourFood.Catalog.Api`  
**Database:** `FindYourFood_CatalogDb`

Responsibilities:

- Food categories.
- Menus.
- Items.
- Prices.
- Add-ons.
- Variants.
- Item availability.
- Food tags.
- Ingredients.
- Dietary labels.
- Allergen details.
- Menu versioning.

Catalog must never write directly into ProviderDB.

## 5.6 Discovery Service

**API:** `FindYourFood.Discovery.Api`  
**Database:** `FindYourFood_DiscoveryDb`

Responsibilities:

- Restaurant search.
- Nearby restaurants.
- Cuisine filters.
- Food search.
- Open restaurant discovery.
- Serviceability-aware ranking.
- Customer preference ranking.
- Search projections.
- Search index refresh.

Discovery can maintain its own read-optimized projection of published Provider and Catalog data through events.

A search projection is not the authoritative source for checkout prices or inventory.

## 5.7 Location & Routing Service

**API:** `FindYourFood.Location.Api`  
**Database:** `FindYourFood_LocationDb`

Responsibilities:

- Service areas.
- Geographic coordinates.
- Geocoding abstraction.
- Road-distance estimates.
- Route provider adapters.
- Traffic provider adapters.
- Delivery zone validation.
- Distance matrices.
- Route estimate caching.
- Location data retention policies.

Use SQL Server spatial capabilities where appropriate.

## 5.8 Cart & Pricing Service

**API:** `FindYourFood.Cart.Api`  
**Database:** `FindYourFood_CartDb`

Responsibilities:

- Shopping carts.
- Guest carts.
- Pricing calculations.
- Delivery-fee quotation.
- Coupon evaluation.
- Checkout estimates.
- Price validation requests.
- Promotion compatibility rules.

Cart pricing is provisional until authoritative checkout validation.

## 5.9 Ordering Service

**API:** `FindYourFood.Ordering.Api`  
**Database:** `FindYourFood_OrderingDb`

Responsibilities:

- Order creation.
- Order snapshots.
- Order state machine.
- Cancellations.
- Checkout orchestration.
- Order-level idempotency.
- Order history.
- Cross-service order saga.
- Compensation workflows.
- Order event publication.

Ordering owns the authoritative order record.

## 5.10 Delivery & Dispatch Service

**API:** `FindYourFood.Delivery.Api`  
**Database:** `FindYourFood_DeliveryDb`

Responsibilities:

- Rider profiles relevant to delivery operations.
- Rider verification status.
- Availability.
- Delivery assignments.
- Dispatch scoring.
- Rider pickup.
- Delivery tracking.
- Delivery confirmation.
- Reassignment.
- Failed delivery handling.
- Location update history.
- Delivery-related audit.

## 5.11 Payment Service

**API:** `FindYourFood.Payment.Api`  
**Database:** `FindYourFood_PaymentDb`

Responsibilities:

- Payment intents.
- Payment attempts.
- Payment confirmation.
- Payment gateway integrations.
- Secure webhooks.
- Refund execution.
- Transaction state.
- Gateway reconciliation.

Never trust a payment success message supplied only by Angular.

## 5.12 Finance & Settlement Service

**API:** `FindYourFood.Finance.Api`  
**Database:** `FindYourFood_FinanceDb`

Responsibilities:

- Restaurant commissions.
- Rider earnings.
- Platform fees.
- Payout calculations.
- Settlement batches.
- Financial journals.
- Double-entry or appropriately balanced ledger records.
- Reconciliation.
- Financial reports.
- Payout approvals.

Do not move money merely because a UI button was clicked.

## 5.13 Notification Service

**API:** `FindYourFood.Notification.Api`  
**Database:** `FindYourFood_NotificationDb`

Responsibilities:

- In-app notifications.
- Order alerts.
- Notification templates.
- Notification preferences.
- Multilingual messages.
- Web push.
- Optional SMS/WhatsApp/email adapters.
- Delivery attempts.
- Retries.
- Scheduling.
- Deduplication.
- Quiet hours.

## 5.14 Dynamic Experience Service

**API:** `FindYourFood.Experience.Api`  
**Database:** `FindYourFood_ExperienceDb`

Responsibilities:

- Website themes.
- Color palettes.
- Typography tokens.
- Page layouts.
- Banners.
- Component registry configurations.
- Feature flags.
- Dynamic menus.
- Page configuration versions.
- Draft/preview/publish/rollback.
- Role-specific website experiences.

## 5.15 Festival & Campaign Service

**API:** `FindYourFood.Campaign.Api`  
**Database:** `FindYourFood_CampaignDb`

Responsibilities:

- Festival calendar.
- Occasions.
- Scheduled promotions.
- Regional campaigns.
- Birthday campaigns.
- Festival food collections.
- Approved campaign rules.
- Campaign priorities.
- Campaign schedules.
- Campaign analytics events.

## 5.16 Reviews & Support Service

**API:** `FindYourFood.Support.Api`  
**Database:** `FindYourFood_SupportDb`

Responsibilities:

- Ratings.
- Reviews.
- Complaints.
- Customer support tickets.
- Restaurant/rider disputes.
- Refund requests.
- Escalations.
- Resolution tracking.

If reviews and support become operationally independent, they may be separated later.

Do not split services merely to maximize their count.

## 5.17 Audit & Operational Reporting

Create appropriately bounded independent components for:

- Security audit records.
- Administrative audit trails.
- Operational reporting.
- Asynchronous analytical projections.

An independent Audit or Analytics service may be introduced when justified.

Business-critical authorization must not depend on an asynchronous analytics pipeline.

---

# SECTION 6 — CLEAN ARCHITECTURE FOR EVERY SERVICE

Every persistent business service must follow this pattern.

Example:

```text
services/
└── Ordering/
    ├── FindYourFood.Ordering.slnx
    │
    ├── src/
    │   ├── FindYourFood.Ordering.Api/
    │   │   ├── Endpoints/
    │   │   ├── Middleware/
    │   │   ├── Configuration/
    │   │   └── Program.cs
    │   │
    │   ├── FindYourFood.Ordering.Application/
    │   │   ├── Abstractions/
    │   │   ├── Commands/
    │   │   ├── Queries/
    │   │   ├── DTOs/
    │   │   └── Behaviors/
    │   │
    │   ├── FindYourFood.Ordering.Domain/
    │   │   ├── Aggregates/
    │   │   ├── Entities/
    │   │   ├── ValueObjects/
    │   │   ├── DomainEvents/
    │   │   └── Rules/
    │   │
    │   └── FindYourFood.Ordering.Infrastructure/
    │       ├── Persistence/
    │       ├── Migrations/
    │       ├── Messaging/
    │       ├── ExternalClients/
    │       ├── Caching/
    │       └── DependencyInjection/
    │
    └── tests/
        ├── FindYourFood.Ordering.UnitTests/
        ├── FindYourFood.Ordering.IntegrationTests/
        └── FindYourFood.Ordering.ContractTests/
```

Apply equivalent conventions to each service, replacing names and adding only relevant feature folders.

Dependency direction:

- Domain has no dependency on other architectural layers.
- Application depends on Domain.
- Infrastructure implements Application abstractions.
- API acts as composition root and transport adapter.
- Independent services do not reference one another's Domain or Infrastructure assemblies.

Create a limited shared building-block package only for stable technical primitives.

Examples:

- Correlation ID utilities.
- Common telemetry configuration.
- Versioned integration-event envelopes.
- Standard ProblemDetails conventions.

Do not share EF Core entities, DbContexts, business aggregates or internal domain models between services.

---

# SECTION 7 — DATABASE PER SERVICE: STRICT RULES

Use separate SQL Server databases for each business microservice.

It is acceptable for all development databases to run under one local SQL Server instance.

For example:

```text
Local SQL Server Instance
├── FindYourFood_IdentityDb
├── FindYourFood_OrganizationDb
├── FindYourFood_CustomerDb
├── FindYourFood_ProviderDb
├── FindYourFood_CatalogDb
├── FindYourFood_DiscoveryDb
├── FindYourFood_LocationDb
├── FindYourFood_CartDb
├── FindYourFood_OrderingDb
├── FindYourFood_DeliveryDb
├── FindYourFood_PaymentDb
├── FindYourFood_FinanceDb
├── FindYourFood_NotificationDb
├── FindYourFood_ExperienceDb
├── FindYourFood_CampaignDb
└── FindYourFood_SupportDb
```

Create databases only when their services are being implemented.

**Strict prohibition:** No service may directly query or modify another service's database.

No cross-service SQL joins.

No cross-database foreign-key dependencies.

Each service owns:

- DbContext.
- Tables.
- Indexes.
- Transactions.
- Schema migrations.
- Data validation.
- Database backup requirements.

Use unique service-specific SQL credentials and least-privilege grants when practical.

For local development, verify SQL Server's edition and available resources.

Use SQL Server Developer for development and testing when permitted.

Do not use Developer edition as a production database licence.

Cross-service relationships must use stable IDs, API calls and event-driven projections.

Where data must be available across services, create intentionally duplicated read models maintained through versioned events.

Design for eventual consistency.

---

# SECTION 8 — YARP API GATEWAY

Create a dedicated project:

`FindYourFood.ApiGateway`

Use YARP Reverse Proxy.

The gateway must:

- Route public requests to the appropriate services.
- Apply suitable authentication policies.
- Apply rate-limiting policies.
- Support load balancing.
- Propagate correlation and tracing headers.
- Enforce request size limits.
- Manage safe forwarding headers.
- Support health-aware destinations.
- Apply suitable timeout policies.
- Emit metrics and traces.
- Version public API routes.

Example logical paths:

```text
/api/v1/identity/**
/api/v1/organization/**
/api/v1/customers/**
/api/v1/providers/**
/api/v1/catalog/**
/api/v1/discovery/**
/api/v1/locations/**
/api/v1/carts/**
/api/v1/orders/**
/api/v1/deliveries/**
/api/v1/payments/**
/api/v1/finance/**
/api/v1/notifications/**
/api/v1/experience/**
/api/v1/campaigns/**
/api/v1/support/**
```

These paths are illustrative and must be mapped to actual internal service routes.

Do not allow the public gateway to access internal administrative endpoints without the required policies.

Do not make the gateway a giant business-logic application.

Avoid unnecessary sequential service calls through the gateway.

Internal service-to-service calls should use secure, well-defined internal communication paths.

Individual microservices must validate authorization; gateway validation alone is insufficient.

---

# SECTION 9 — SERVICE COMMUNICATION

Use two communication approaches.

## 9.1 Synchronous communication

Use HTTP/REST where an immediate answer is genuinely required.

Consider gRPC for high-throughput internal calls when measured performance and deployment support justify it.

Use:

- Typed HttpClient clients.
- Connection reuse.
- Timeouts.
- Cancellation tokens.
- Resilience pipelines.
- Circuit breakers.
- Bounded retries.
- Correlation IDs.
- Secure service authentication.
- Service discovery or centrally configured endpoints.

Do not retry non-idempotent operations blindly.

Avoid long chains such as:

```text
Gateway -> A -> B -> C -> D -> E
```

Design service APIs to prevent excessive request fan-out.

## 9.2 Asynchronous communication

Use integration events and durable messaging for workflows that do not require immediate synchronous completion.

Examples:

```text
CustomerRegistered
ProviderApproved
MenuPublished
RestaurantAvailabilityChanged
OrderPlaced
OrderAccepted
FoodPreparationStarted
RiderAssigned
FoodPickedUp
OrderDelivered
PaymentCaptured
PaymentFailed
RefundCompleted
CampaignPublished
```

Events describe facts that have already happened.

Commands express requested actions.

Do not confuse these semantics.

Every event must have:

- Unique message ID.
- Event type.
- Schema version.
- Timestamp.
- Correlation ID.
- Causation ID where appropriate.
- Aggregate/entity ID.
- Producer identification.
- Validated payload.

Design consumers to handle repeated and out-of-order messages.

---

# SECTION 10 — RABBITMQ ARCHITECTURE

RabbitMQ will be our primary reliable business-work messaging system where task-oriented asynchronous behavior is needed.

Possible use cases:

- Sending notifications.
- Running delivery-assignment jobs.
- Processing noncritical post-order work.
- Scheduling retryable operations.
- Sending operational commands.
- Triggering certain saga steps.

Use:

- Durable messaging patterns.
- Appropriate queues/exchanges.
- Publisher acknowledgements.
- Consumer acknowledgements.
- Controlled prefetch.
- Dead-letter queues.
- Retry limits and backoff.
- Idempotent consumers.
- Consumer failure monitoring.
- Correlation and trace propagation.

Consider MassTransit only after verifying current commercial licensing and architectural benefit. A maintained native RabbitMQ client is an acceptable alternative.

RabbitMQ must not become the authoritative store of orders or financial balances.

SQL Server remains authoritative for durable business state.

## Local Windows development

RabbitMQ can be installed on Windows with a compatible Erlang runtime.

However, installation and startup must be optional until a feature requires real broker integration.

Provide documented native startup/stop commands and a resource-light test configuration.

If RabbitMQ is unavailable:

- Do not fake a successful broker integration test.
- Use a separate, clearly identified development transport for unaffected features.
- Keep durable outbox records.
- Mark real-broker integration as pending.
- Continue independent development.

---

# SECTION 11 — KAFKA ARCHITECTURE

Kafka is required as a planned platform integration, but should be introduced where replayable event streams provide a genuine advantage.

Potential use cases:

- Order lifecycle event streams.
- Restaurant analytics.
- Search projection updates.
- Operational analytics.
- Rider activity analysis.
- High-volume business event processing.
- Rebuilding analytical read models.
- Future recommendation pipelines.

Use current supported Kafka KRaft architecture.

Do not introduce ZooKeeper-based Kafka deployment unless there is a separately justified compatibility requirement.

Design:

- Topic naming conventions.
- Event schema versions.
- Partition keys.
- Ordering guarantees per key.
- Consumer groups.
- Retention policies.
- Retry handling.
- Dead-letter handling where applicable.
- Event replay.
- Access controls.
- Consumer lag monitoring.

Choose partition keys deliberately, such as order ID where per-order ordering is needed.

Avoid assuming global ordering across all Kafka partitions.

Do not claim exactly-once end-to-end business processing merely because Kafka has exactly-once features.

Use database idempotency and consumer deduplication.

## RabbitMQ vs Kafka

Do not blindly publish each event to both systems.

Document which channel owns each workload.

Example:

- RabbitMQ: notification-delivery work command.
- Kafka: replayable order lifecycle analytics event.

Where an event needs both an operational workflow and an analytical stream, publish through an explicit, reliable integration strategy.

Do not create cyclic bridges.

## Local development

Kafka can be resource-heavy, especially alongside many APIs and SQL Server.

Do not make Kafka mandatory for basic CRUD development or every microservice test.

Provide an optional dedicated Kafka integration profile.

Document how to run a supported Kafka broker without Docker on an appropriate environment.

If the native Windows configuration is unsuitable, record the limitation and defer full Kafka integration testing to an approved capable environment.

Never silently substitute a fake test and report Kafka as integrated.

---

# SECTION 12 — REDIS / VALKEY CACHING

Implement a distributed cache abstraction with a Redis-compatible adapter.

Prefer Valkey where its permissive BSD licensing is desirable.

Redis is also an option after reviewing the applicable Redis distribution and licensing.

Use Redis-compatible caching for:

- Popular restaurant listings.
- Food categories.
- Published dynamic configurations.
- Festival configuration.
- Read-heavy discovery results.
- Temporary rate-limit counters.
- Certain distributed synchronization needs when correctly designed.
- Short-lived location-related hints where appropriate.

Do not treat Redis as the authoritative database for:

- Orders.
- Payments.
- Financial balances.
- Final rider assignments.
- Refund decisions.

Use:

- Cache-aside where appropriate.
- Explicit TTL.
- Versioned cache keys.
- Event-driven invalidation.
- Cache stampede protection.
- Bounded key cardinality.
- Proper memory limits.
- Metrics.

Do not cache private customer data in publicly shared keys.

Do not assume Redis/Valkey provides strongly consistent distributed locking for critical financial operations.

Use database transactions and constraints for authoritative concurrency control.

On Windows without Docker, verify whether the selected cache server is supported natively. Otherwise provide a proper no-cache or in-process development profile while retaining the distributed cache adapter for full integration testing.

The application must degrade safely if cache connectivity fails.

---

# SECTION 13 — DISTRIBUTED TRANSACTIONS AND SAGA PATTERN

Because each microservice owns a separate database, do not attempt ordinary cross-service EF Core transactions.

Implement distributed workflow coordination.

For critical orders, use an explicitly documented saga.

Example order flow:

```text
CheckoutRequested
        |
        v
Ordering creates Pending Order
        |
        v
Catalog / Provider availability checked
        |
        v
Price and serviceability confirmed
        |
        v
Payment authorization or COD validation
        |
        v
Restaurant confirmation
        |
        v
Order Confirmed
        |
        v
Delivery assignment
        |
        v
Pickup and delivery
        |
        v
Settlement / completion
```

This is a conceptual flow. Design precise payment capture, restaurant acceptance, cancellation and refund sequencing based on gateway capabilities and business policy.

Consider orchestrated sagas for complex financial/order workflows.

Define:

- Every step.
- Expected response.
- Timeout.
- Retry behavior.
- Compensation.
- Terminal failure state.
- Manual recovery process.

Examples:

- Restaurant rejects after payment authorization: void authorization or initiate refund as appropriate.
- Payment succeeds but the API loses its response: reconcile with the gateway before retrying.
- Delivery becomes unavailable: reassign, negotiate a new ETA, switch to an approved alternative or cancel according to policy.
- Duplicate event arrives: consume idempotently.
- One service is down: retain durable state and resume safely.

Use transactional outbox in event-producing services.

Use inbox/deduplication in event-consuming services.

Implement durable worker recovery.

Do not use an in-memory message queue as the only durable mechanism for order-critical events.

---

# SECTION 14 — IDENTITY AND ENTERPRISE AUTHORIZATION

Support these participants:

- Customers.
- Guests.
- Food providers.
- Restaurant staff.
- Delivery partners.
- Owner.
- CEO.
- Department heads.
- City managers.
- Team managers.
- Employees.
- Support agents.
- Finance staff.
- Operations staff.

## 14.1 Secure authentication

Implement:

- Secure password hashing.
- Optional OTP mechanisms.
- MFA for privileged users.
- Token expiration.
- Refresh-token rotation.
- Session revocation.
- Account lockout.
- Secure password recovery.
- Token signing-key management.
- Secure service-to-service authentication.

Use OIDC/JWT standards appropriately.

Public services must validate issuer, audience, signature and other relevant token claims.

Do not share a hardcoded symmetric secret across all microservices.

Prefer managed key rotation and public-key validation/JWKS where appropriate.

## 14.2 Owner hierarchy

Support:

```text
Owner
  |
  CEO
  |
  Department Head
  |
  Regional/City Manager
  |
  Team Manager
  |
  Employee
```

The hierarchy is configurable and does not automatically imply unlimited access.

Use:

- RBAC.
- Fine-grained action permissions.
- Organizational resource scopes.
- Attribute-based authorization.
- Relationship-based checks for manager/subordinate access.
- Default-deny policies.

A manager can only delegate explicitly delegable permissions they possess, and only within their own scope.

A CEO cannot grant owner-only permissions.

No employee can elevate their own authority.

No manager can create or control a superior outside approved workflows.

## 14.3 Distributed authorization

Organization Service is the permission authority.

Other microservices must enforce authorization locally using verified claims and/or securely obtained permission decisions.

Design an authorization library/contract that supports:

- Scoped permission evaluation.
- Permission-version changes.
- Explicit revocation.
- Token expiry.
- Decision caching where safe.
- Fail-closed behavior for sensitive mutations.

Do not rely on a stale role claim alone for high-risk operations such as changing payout details or granting administrator permissions.

Do not make an authorization outage silently grant access.

Use clearly defined read fallback policies only where safe.

## 14.4 Owner dashboard

The owner must be able to:

- Manage authorized executives.
- Review employee roles.
- Review permissions and delegations.
- View operational performance.
- Manage restaurant approvals.
- Manage riders and delivery operations.
- Manage commissions and settings.
- Manage themes and campaigns.
- Review audit history.
- Assign tasks to authorized staff.
- Revoke access.
- Inspect business and financial reports.

Highly sensitive data and secrets should remain protected even from ordinary dashboard browsing.

## 14.5 Audit history

Audit:

- Actor.
- Resource.
- Action.
- Scope.
- Timestamp.
- Correlation ID.
- Permission change.
- Before/after values where safe.
- Approval records.

Do not log raw passwords, access tokens, payment credentials or other secrets.

---

# SECTION 15 — GEOLOCATION, NEARBY RESTAURANTS AND SEARCH

When a customer opens FindYourFood, display suitable nearby restaurants by default.

Use the customer's chosen address or permission-based location.

Do not require GPS permission.

Provide manual address selection.

Default ranking should consider:

1. Restaurant verification.
2. Serviceability.
3. Current opening hours.
4. Food availability.
5. Geographic distance.
6. Road-based ETA when available.
7. Restaurant preparation time.
8. Customer-selected preferences.
9. Relevant quality signals.
10. Configurable marketplace ranking policy.

Use SQL Server geography features and spatial indexing where appropriate.

For location queries:

- Filter by service zone.
- Prefilter nearby candidates.
- Rank bounded candidate sets.
- Avoid routing calculations for every restaurant.
- Use cache carefully.
- Paginate results.
- Provide freshness indicators for operational data.

Discovery results must not replace authoritative checkout validation.

Support filters:

- Nearby.
- Fastest delivery.
- Vegetarian.
- Non-vegetarian.
- Jain-friendly.
- Price.
- Rating.
- Cuisine.
- Offers.
- Open now.
- Pickup.
- Delivery.

Search should respect explicit customer filters.

Avoid excessive personalization that hides relevant options without explanation.

---

# SECTION 16 — SMART RIDER ASSIGNMENT

FindYourFood must choose an appropriate rider using estimated pickup time rather than simple straight-line distance alone.

The dispatch engine must consider:

- Rider online status.
- Approval status.
- Rider location freshness.
- Current assignment count.
- Delivery capacity.
- Rider-to-restaurant distance.
- Road-network travel time.
- Food preparation readiness.
- Restaurant-to-customer distance.
- Available traffic information.
- Service-zone constraints.
- Delivery reliability.
- Fairness and operational workload.

Example:

```text
Restaurant A has prepared an order.

Rider 1:
  0.5 km away
  Road ETA: 12 minutes

Rider 2:
  1.2 km away
  Road ETA: 5 minutes

Rider 2 may be the better choice.
```

The dispatch engine must use actual route estimates when available.

Never label an approximation as live traffic.

## 16.1 Dispatch workflow

```text
Order Accepted
   |
   v
Food Readiness Estimate
   |
   v
Eligible Nearby Riders
   |
   v
Route and Pickup ETA
   |
   v
Dispatch Ranking
   |
   v
Offer / Reserve Rider
   |
   v
Rider Acceptance
   |
   v
Atomic Assignment
   |
   v
Pickup
   |
   v
Delivery
```

Handle:

- Rider rejection.
- Rider going offline.
- Expired offers.
- Slow rider response.
- Rider cancellation.
- Restaurant preparation delay.
- Traffic delay.
- Delivery failure.
- Reassignment.
- Customer cancellation.

Use transactions and concurrency control to avoid double assignments.

## 16.2 Real-time updates

Use SignalR for customer/provider/rider live updates where appropriate.

Provide a secure REST status fallback when the real-time connection is unavailable.

Do not expose other riders' locations.

Do not expose customer information outside authorized delivery assignments.

Continuous background location tracking may require future native rider applications.

The browser-based rider application must not promise reliable background tracking when the browser is suspended.

---

# SECTION 17 — MAPS, ROAD ROUTING AND LIVE TRAFFIC

Use abstractions:

```csharp
IGeocodingProvider
IRoutingProvider
IRouteMatrixProvider
ITrafficProvider
IDeliveryEtaCalculator
IServiceAreaResolver
```

Candidate tools:

- MapLibre for map rendering.
- OpenStreetMap-based data with proper attribution.
- OSRM or another appropriate routing engine.
- Future licensed traffic provider where necessary.

Do not misuse public community map or geocoding infrastructure for unrestricted commercial traffic.

Live traffic cannot be assumed to be free.

Provide conservative ETA estimates when real traffic information is unavailable.

Support:

- Cacheable routes.
- Route freshness.
- Traffic-provider availability.
- ETA uncertainty.
- Recalculation triggers.
- Safe provider fallback.
- Provider-specific rate limits.

Do not expose private exact delivery locations longer than needed.

---

# SECTION 18 — BACKEND-DRIVEN DYNAMIC ANGULAR DESIGN

Create a Dynamic Experience Service.

The owner should be able to change supported design configurations without touching frontend code.

Support:

- Themes.
- Colors.
- Fonts.
- Logo.
- Banners.
- Menus.
- Homepage sections.
- Component ordering.
- Promotional areas.
- Food collections.
- Restaurant carousels.
- City-specific designs.
- Role-specific page layouts.
- Festival designs.
- Feature flags.
- Configurable content.

Use a versioned JSON schema.

Angular must render only registered, trusted components.

Do not execute arbitrary JavaScript or Angular templates returned by the backend.

Use:

```text
Draft
 -> Validate
 -> Preview
 -> Approve
 -> Publish
 -> Version
 -> Rollback
```

Add caching and safe fallback themes.

If Experience Service is unavailable, public pages must still render with the last valid cached configuration or a bundled default.

New frontend functionality may still require implementation, but existing registered components should be configurable through the admin portal.

---

# SECTION 19 — FESTIVAL CALENDAR AND DIETARY MODES

Implement Calendar & Campaign Service.

Supported occasions include:

- Diwali.
- Holi.
- Navratri.
- Christmas.
- Eid.
- Regional festivals.
- Birthdays.
- Anniversaries.
- Restaurant events.
- Local celebrations.
- Owner-created campaigns.

Support:

- Start dates.
- End dates.
- Offset schedules.
- Time zones.
- Locations.
- Campaign priority.
- Themes.
- Offers.
- Food collections.
- Notification templates.
- Automatic expiration.
- Manual overrides.
- Audit history.

Verify movable festival dates annually.

Do not use hardcoded fixed Gregorian recurring dates for lunar-calendar observances.

## Dietary controls

Customer-selected modes:

- All food.
- Vegetarian.
- Egg-free.
- No onion/garlic.
- Jain-friendly.
- Fasting-specific options.
- Other explicitly selected restrictions.

Do not infer religion from a customer name or location.

Do not hide non-vegetarian food simply because a festival exists.

Use customer opt-in to activate applicable food filters.

Food-provider claims must be validated appropriately, and uncertain ingredient compatibility must be displayed honestly.

---

# SECTION 20 — CREATIVE NOTIFICATION ENGINE

Make FindYourFood friendly and distinctive.

Support:

- Order updates.
- Rider updates.
- Delivery confirmation.
- Refund updates.
- Festival greetings.
- Birthday messages.
- Offers.
- Helpful meal reminders.
- Funny Hinglish messages.
- Regional-language variations.

Example:

"Pet ka API timeout ho gaya? Kuch tasty order kar lo! 🍛"

Do not copy competitors' exact marketing content.

Support:

- Notification templates.
- Language.
- Customer consent.
- Frequency caps.
- Quiet hours.
- Scheduling.
- Deduplication.
- Campaign targeting.
- In-app inbox.
- Browser push when permission is granted.
- Optional email and SMS providers.

Separate essential transactional notifications from optional marketing notifications.

---

# SECTION 21 — FOOD PROVIDERS AND CATALOG

Allow eligible restaurants, dhabas, cloud kitchens, bakeries, cafes and home kitchens to join.

Provider flow:

```text
Registration
 -> Document Submission
 -> Verification
 -> Approval
 -> Branch Setup
 -> Menu Setup
 -> Publication
 -> Order Acceptance
```

Manage:

- Menu categories.
- Items.
- Prices.
- Variants.
- Add-ons.
- Operating hours.
- Availability.
- Preparation time.
- Taxes and fees.
- Food labels.
- Allergen information.
- Branch service areas.
- Restaurant staff permissions.

Keep Provider and Catalog data ownership separate.

Use events to update Discovery Service search projections.

---

# SECTION 22 — ORDERS, PAYMENTS AND FINANCE

Support:

- Registered checkout.
- Guest checkout.
- Cash on delivery where allowed.
- Gateway-based online payment.
- Server-side price calculation.
- Checkout idempotency.
- Order snapshots.
- Payment attempts.
- Payment webhook verification.
- Cancellation.
- Refunds.
- Restaurant commission.
- Rider payouts.
- Platform revenue.
- Settlement records.
- Financial reconciliation.

Use a clear, auditable money model.

Do not treat a payment authorization, capture, refund and settlement as the same operation.

Handle partial failures and out-of-order callbacks.

Create explicit state transitions and reconciliation jobs.

Never store card secrets.

Do not activate real billable payment transactions until approved.

For development, use an isolated payment simulator with clearly labeled simulated results.

---

# SECTION 23 — COMPLETE ANGULAR APPLICATIONS

Use one Angular workspace with independently buildable applications.

## 23.1 Customer Web

- Homepage.
- Location selection.
- Nearby restaurant listing.
- Search.
- Restaurant details.
- Menu.
- Food filtering.
- Cart.
- Guest checkout.
- Login/register.
- Order tracking.
- Order history.
- Offers.
- Favorites.
- Reviews.
- Support.
- Profile.
- Notification settings.

## 23.2 Provider Portal

- Registration.
- Verification.
- Dashboard.
- Branch management.
- Food menu management.
- Orders.
- Preparation queue.
- Food availability.
- Staff management.
- Earnings.
- Offers.
- Restaurant settings.

## 23.3 Rider Portal

- Registration.
- Verification.
- Dashboard.
- Go online/offline.
- Assignment offers.
- Active delivery.
- Pickup confirmation.
- Navigation.
- Delivery confirmation.
- Earnings.
- Support.
- History.

## 23.4 Owner/Admin Portal

- Executive dashboard.
- CEO management.
- Employee management.
- Organization hierarchy.
- Permissions.
- Department management.
- Provider approvals.
- Rider approvals.
- Location/service areas.
- Order monitoring.
- Dispatch operations.
- Finance.
- Refunds.
- Themes.
- Dynamic UI editor.
- Festival calendar.
- Campaigns.
- Notifications.
- Customer support.
- Analytics.
- Audit history.
- Platform settings.

Use reusable shared UI components, typed API clients and consistent design tokens.

Frontend permission checks enhance UX but do not replace backend authorization.

Provide responsive designs, accessibility, loading states, empty states and error handling.

Build each Angular feature alongside its corresponding backend functionality.

---

# SECTION 24 — ADDITIONAL ADVANCED ENGINEERING TOOLS

Evaluate tools based on actual need and licence.

## Resilience

Use .NET resilience pipelines/Polly for:

- Retry.
- Timeout.
- Circuit breaker.
- Concurrency limits.
- Safe fallback.

## Service discovery

Use explicit development endpoints initially.

Consider Aspire service discovery or an appropriate discovery mechanism later.

Do not require Consul or Kubernetes just to make two local APIs communicate.

## Distributed tracing

Use OpenTelemetry and W3C trace context propagation.

Trace flows such as:

```text
Customer API request
 -> Gateway
 -> Ordering
 -> Payment
 -> Restaurant
 -> Dispatch
 -> Notification
```

Include async event correlation.

## API contract management

Use:

- OpenAPI.
- Contract versioning.
- Backward compatibility checks.
- Consumer/provider contract tests.
- Event schema compatibility tests.

Do not break existing consumers silently.

## Background processing

Use reliable hosted workers with durable persistence and broker integration where needed.

Avoid adding multiple job-processing frameworks with overlapping responsibilities.

## Scheduling

Evaluate Quartz.NET or equivalent maintained scheduling technology for campaign automation.

Use distributed coordination where needed to avoid duplicate job execution.

## Security

Implement:

- Rate limiting.
- Secure CORS.
- Input validation.
- Output encoding.
- Authorization policies.
- Secret management.
- Audit history.
- Dependency vulnerability checks.
- Security scanning.
- Least privilege.

## Performance

Use:

- Efficient EF Core queries.
- AsNoTracking where appropriate.
- Projections.
- Spatial indexes.
- Keyset pagination where appropriate.
- Query plans.
- Controlled caching.
- Async I/O.
- Connection pooling.
- Low-allocation patterns where measured.

Avoid premature optimization that reduces clarity without evidence.

---

# SECTION 25 — OBSERVABILITY AND OPERATIONS

Every microservice must emit:

- Structured logs.
- Traces.
- Metrics.
- Health status.
- Readiness status.
- Dependency health.
- Error counters.
- Relevant business metrics.

Monitor:

- API latency.
- SQL performance.
- Authentication failures.
- RabbitMQ queue depth.
- Kafka consumer lag.
- Cache hit rate.
- Order creation failures.
- Payment errors.
- Dispatch latency.
- Delivery completion.
- Notification failures.

Use free/open-source tools for development.

Do not require a full Prometheus/Grafana/Jaeger stack to run every simple local test.

Use lightweight telemetry exporters during ordinary development and enable dashboards in dedicated integration profiles.

---

# SECTION 26 — SCALABILITY OBJECTIVES

The platform may eventually aim for extremely large user populations.

Design for horizontal scale.

Do not claim that the system can support 100 crore concurrent users without measured evidence.

Implement:

- Stateless service instances.
- Independently deployable services.
- Rate limiting.
- Backpressure.
- Bounded concurrency.
- Connection pool protection.
- Cache scaling.
- Efficient SQL.
- Message queue capacity management.
- Replay/recovery.
- Autoscaling readiness.
- Circuit breakers.
- Failure isolation.
- Graceful degradation.
- Distributed monitoring.

Future deployment may require:

- Multiple API instances.
- Distributed SQL infrastructure.
- Read replicas where appropriate.
- Partitioning/sharding where justified.
- Multi-region architecture.
- CDN.
- WAF.
- Load balancers.
- Managed infrastructure.
- Disaster recovery.
- Advanced caching.
- Large-scale event processing.

These production capabilities may cost money.

Do not suggest that a free local laptop can operate at hyperscale.

## Load testing

Use k6.

Test realistic business scenarios, not only simple health endpoints.

Examples:

- Concurrent restaurant searches.
- Many customer carts.
- Concurrent order placement.
- Duplicate payment callbacks.
- Rapid rider location updates.
- Large notification campaigns.
- Restaurant menu publication.
- Cache failures.
- Payment-provider outage.
- Broker outages.

Measure throughput, p95/p99 latency, errors and recovery behavior.

Record the exact environment and test results.

---

# SECTION 27 — LOCAL WINDOWS DEVELOPMENT WITHOUT DOCKER

Create PowerShell scripts:

```text
scripts/
├── Start-IdentityEnvironment.ps1
├── Start-ProviderEnvironment.ps1
├── Start-OrderingEnvironment.ps1
├── Start-DeliveryEnvironment.ps1
├── Start-Frontend.ps1
├── Start-IntegrationEnvironment.ps1
├── Stop-DevelopmentServices.ps1
├── Test-Services.ps1
└── Check-Prerequisites.ps1
```

These scripts must use the actual solution paths and ports.

They must fail clearly if prerequisites are missing.

Do not create misleading scripts that silently skip required integration tests.

Use service-specific profiles to limit memory consumption.

Support a simple manual `dotnet run` workflow for each service.

Document:

- SQL Server connection setup.
- Database initialization.
- Per-service migrations.
- Port allocation.
- HTTPS certificates.
- Angular start commands.
- Optional broker setup.
- Optional cache setup.
- How to run specific tests.
- How to stop all local processes.

For an unavailable optional dependency, provide a documented development adapter or defer the affected integration test.

Never claim a production-level broker/cache integration is verified when it is not.

---

# SECTION 28 — REPOSITORY STRUCTURE

Use a monorepo for coordinated source management while preserving independently deployable microservices.

**A monorepo is not a monolith.**

Suggested root:

```text
FindYourFood/
│
├── AGENTS.md
├── README.md
├── .gitignore
├── .editorconfig
│
├── docs/
│   ├── requirements/
│   ├── architecture/
│   ├── microservices/
│   ├── databases/
│   ├── security/
│   ├── api-contracts/
│   ├── events/
│   ├── testing/
│   ├── deployment/
│   ├── decisions/
│   ├── WORK_PLAN.md
│   ├── PROGRESS.md
│   └── BLOCKERS.md
│
├── backend/
│   ├── gateway/
│   │   └── FindYourFood.ApiGateway/
│   │
│   ├── services/
│   │   ├── Identity/
│   │   ├── Organization/
│   │   ├── Customer/
│   │   ├── Provider/
│   │   ├── Catalog/
│   │   ├── Discovery/
│   │   ├── Location/
│   │   ├── Cart/
│   │   ├── Ordering/
│   │   ├── Delivery/
│   │   ├── Payment/
│   │   ├── Finance/
│   │   ├── Notification/
│   │   ├── Experience/
│   │   ├── Campaign/
│   │   └── Support/
│   │
│   ├── building-blocks/
│   │   ├── Observability/
│   │   ├── Messaging.Contracts/
│   │   └── Api.Conventions/
│   │
│   └── integration-tests/
│
├── frontend/
│   ├── apps/
│   │   ├── customer-web/
│   │   ├── provider-portal/
│   │   ├── rider-portal/
│   │   └── admin-portal/
│   └── shared/
│
├── infrastructure/
│   ├── gateway/
│   ├── messaging/
│   ├── caching/
│   ├── monitoring/
│   └── deployment/
│
├── scripts/
│
└── tests/
    ├── e2e/
    ├── contracts/
    ├── performance/
    └── security/
```

Do not create hundreds of empty projects in one pass.

Create the complete architectural plan, then implement the service projects incrementally.

Each created business microservice must be genuinely independently runnable and have its own private database.

---

# SECTION 29 — DEVELOPMENT EXECUTION ROADMAP

Follow the sequence below.

## Phase 0: Environment audit

Inspect the existing FindYourFood repository and development environment.

Check:

- .NET SDK.
- Angular/Node.js compatibility.
- SQL Server.
- Available memory/disk.
- Git status.
- GitHub remote.
- Existing code.
- Available development tools.

Never delete existing code without review.

## Phase 1: Microservices foundation

Create:

- Monorepo structure.
- YARP gateway.
- One real microservice using Clean Architecture.
- Separate SQL Server database.
- OpenAPI.
- Health checks.
- Structured logging.
- API conventions.
- Test projects.
- Angular workspace foundation.

Verify all core components locally without Docker.

## Phase 2: Identity Service

Implement:

- Account creation.
- Authentication.
- Secure owner bootstrap.
- Token issuance.
- Sessions.
- MFA foundation.
- IdentityDb.
- Authentication tests.
- Angular login/register integration.

## Phase 3: Organization Service

Implement:

- Owner/CEO/manager/employee hierarchy.
- Permissions.
- Delegation.
- Role scopes.
- Staff management.
- Auditing.
- Cross-service authorization contracts.
- OrganizationDb.
- Angular admin screens.

Test privilege escalation prevention.

## Phase 4: Experience Service

Implement:

- Dynamic themes.
- Page configurations.
- Component registry.
- Preview/publish.
- Rollback.
- Configuration caching.
- Angular dynamic rendering.

## Phase 5: Provider Service

Implement registration, verification, branches and provider operations.

Build provider portal integration.

## Phase 6: Catalog Service

Implement menus, items, variants and availability.

Build provider menu management and customer menu displays.

## Phase 7: Discovery + Location Services

Implement:

- Customer location.
- Service areas.
- Nearby restaurant queries.
- Search read models.
- Ranking.
- Road-routing abstraction.
- Location fallback.

Build customer discovery frontend.

## Phase 8: Cart + Ordering Services

Implement:

- Cart and checkout.
- Price validation.
- Order creation.
- Order state machine.
- Initial saga orchestration.
- Idempotency.
- Customer tracking pages.

## Phase 9: Messaging foundation

Introduce reliable outbox/inbox integration.

Implement RabbitMQ native integration for an appropriate workflow.

Test actual broker consumption when the broker is available.

Keep Kafka as a separate integration milestone with its own tests and event-stream requirements.

## Phase 10: Payment Service

Implement development payment simulation, payment attempts, gateway abstraction, secure webhook handling and compensation logic.

Build appropriate customer checkout integration.

## Phase 11: Delivery Service

Implement:

- Rider onboarding.
- Rider availability.
- Dispatch algorithm.
- Atomic assignment.
- Route/ETA estimation.
- Pickup.
- Tracking.
- Delivery completion.

Build rider and customer live-tracking interfaces.

## Phase 12: Finance Service

Implement:

- Commissions.
- Rider earnings.
- Settlement journal.
- Refund accounting.
- Financial approval workflows.
- Reconciliation.

## Phase 13: Campaign + Notification Services

Implement:

- Festival calendar.
- Dynamic seasonal configurations.
- Dietary preferences.
- Notification templates.
- Campaign scheduling.
- Customer consent.
- Promotional messages.

## Phase 14: Kafka analytics streams

Implement:

- Versioned event contracts.
- Selected replayable event streams.
- Consumer projections.
- Event ordering tests.
- Replay recovery.
- Consumer lag monitoring.

Do not publish everything to Kafka without purpose.

## Phase 15: Support and owner operations

Complete:

- Customer complaints.
- Restaurant disputes.
- Rider support.
- Reviews.
- Owner dashboards.
- Operational metrics.
- Employee task assignment.
- Audit reports.

## Phase 16: Advanced reliability

Perform:

- Contract tests.
- Distributed integration tests.
- Security reviews.
- Load tests.
- Broker failure testing.
- Redis failure testing.
- SQL recovery testing.
- Routing-provider failure testing.
- Saga compensation testing.
- Documentation review.

## Phase 17: Production architecture preparation

Prepare:

- Deployment strategy.
- Containerization proposal for future approval.
- Service scaling plan.
- Database scaling plan.
- CDN/WAF planning.
- Backup and disaster recovery.
- Production monitoring.
- Security hardening.
- Cost estimates.

Do not implement Docker until approved by the owner.

---

# SECTION 30 — WORK ONE API AND FRONTEND FEATURE AT A TIME

For every feature, follow:

```text
Understand Requirement
        |
        v
Define API/Event Contract
        |
        v
Implement Domain
        |
        v
Implement Application
        |
        v
Implement Infrastructure
        |
        v
Create Database Migration
        |
        v
Implement API
        |
        v
Write Backend Tests
        |
        v
Build Angular Feature
        |
        v
Write Frontend Tests
        |
        v
Integration Verification
        |
        v
Regression Verification
        |
        v
Git Commit
        |
        v
Push if Authorized
        |
        v
Update Progress
        |
        v
Next Feature
```

Do not build the entire backend first and the frontend months later.

Each increment should be vertically integrated and independently verifiable.

A feature is not complete merely because its API compiles.

Do not fake test success.

Do not commit secrets.

---

# SECTION 31 — GITHUB WORKFLOW

Use Git from the beginning.

Check repository state before modifying existing work.

Use meaningful feature branches and commits.

Recommended commit examples:

```text
feat(identity): implement secure customer registration

feat(organization): add scoped permission delegation

feat(discovery): add nearby restaurant filtering

feat(delivery): implement rider assignment workflow

test(ordering): cover duplicate checkout protection
```

Push through configured authenticated GitHub access where authorized.

If repository access, authentication or branch protection prevents pushing:

1. Complete permitted local coding.
2. Run tests.
3. Create local commits.
4. Record the push blocker.
5. Continue independent coding.
6. Ask the owner for the required action.

Do not bypass approvals.

Never force-push over existing work without explicit authorization.

---

# SECTION 32 — OWNER APPROVAL AND NON-BLOCKING POLICY

The owner does not want coding to stop unnecessarily.

Continue with approved technical implementation when:

- Requirements are sufficiently clear.
- No real money is being spent.
- No protected resource is being modified without permission.
- No destructive operation is required.
- A safe reversible decision exists.
- An optional external dependency is unavailable.

Stop only the affected operation if:

- Production credentials are required.
- Payment infrastructure requires billing approval.
- External account permissions are missing.
- A destructive migration is proposed.
- Production deployment is requested without approval.
- A high-risk security decision is ambiguous.
- Repository permissions prohibit an action.

Record blockers in:

`docs/BLOCKERS.md`

Use:

```text
Blocker ID:
Feature:
Description:
Required User Action:
Impact:
Safe Workaround:
Current Status:
```

Do not pretend that Codex can work in the background after its execution session ends.

Within an active authorized session, continue eligible independent tasks.

At the next available session, inspect the progress files and resume the outstanding work.

---

# SECTION 33 — COMMUNICATION AND REPORTING

At the beginning of the work session, explain:

- Current phase.
- Architecture or files inspected.
- Planned changes.
- Service being implemented.
- Tests to run.
- Any important blockers.

At meaningful milestones, report:

```text
FindYourFood Development Update

Current Phase:
Current Microservice:

Backend:
- Completed functionality

Database:
- Database and migration status

Frontend:
- Connected pages/components

Infrastructure:
- Gateway / broker / cache status

Testing:
- Tests executed
- Passed
- Failed
- Not yet executed

Git:
- Commit
- Push status

Blockers:
- Outstanding approvals

Next:
- Next specific feature
```

Keep updates concise and accurate.

Do not provide generic progress messages without actual work.

Do not claim tests passed if they did not run.

---

# SECTION 34 — DOCUMENTATION AND CODEX CONTEXT PRESERVATION

Create `AGENTS.md` and maintain architecture documentation.

Required documents:

```text
docs/
├── requirements/
│   └── PRODUCT_REQUIREMENTS.md
│
├── architecture/
│   ├── SYSTEM_ARCHITECTURE.md
│   ├── MICROSERVICE_BOUNDARIES.md
│   ├── SERVICE_COMMUNICATION.md
│   └── DATA_OWNERSHIP.md
│
├── security/
│   ├── AUTHENTICATION.md
│   ├── AUTHORIZATION.md
│   └── DELEGATION_RULES.md
│
├── events/
│   ├── EVENT_CATALOG.md
│   ├── RABBITMQ_TOPOLOGY.md
│   └── KAFKA_TOPICS.md
│
├── database/
│   ├── DATABASE_CATALOG.md
│   └── MIGRATION_POLICY.md
│
├── testing/
│   └── TEST_STRATEGY.md
│
├── WORK_PLAN.md
├── PROGRESS.md
└── BLOCKERS.md
```

Maintain explicit architecture decision records.

Include:

- Why each microservice exists.
- Which database it owns.
- Which events it publishes.
- Which events it consumes.
- Which APIs it exposes.
- Which external services it depends on.
- What happens when its dependencies fail.

Future Codex sessions must be able to understand project status from repository documentation without recreating the original chat.

---

# SECTION 35 — DEFINITION OF DONE

A microservice feature is complete only when:

1. The correct service owns the feature.
2. Clean Architecture boundaries are respected.
3. Its database ownership rules are followed.
4. Migrations are implemented where needed.
5. Validation is present.
6. Authorization is enforced.
7. API contracts are documented.
8. Integration events are versioned where applicable.
9. Error handling is implemented.
10. Relevant failure scenarios are tested.
11. Unit tests pass.
12. Integration tests pass for available dependencies.
13. Contract tests pass where applicable.
14. Angular integration is complete.
15. Frontend build and tests pass.
16. No secrets are committed.
17. Git changes are committed.
18. GitHub is updated when authorized.
19. Documentation reflects the actual implementation.
20. Remaining integration limitations are clearly stated.

Do not call a service production-ready until real integration, security, operational and performance criteria have been verified.

---

# SECTION 36 — FIRST TASK FOR CODEX

**START IMPLEMENTATION NOW.**

First, inspect the current working directory, repository state, operating system and installed development tools.

Do not assume an empty repository.

Do not delete existing source code.

Then perform these steps in sequence.

### Step 1 — Environment verification

Check:

- .NET 10 SDK.
- Compatible Node.js version.
- Angular 22 requirements.
- SQL Server instance.
- Git availability.
- GitHub configuration.
- Available disk and memory.
- Existing FindYourFood code.

Report blockers without stopping unrelated work.

### Step 2 — Architecture blueprint

Create the complete microservices architecture documentation.

Document:

- Every planned service.
- Database ownership.
- API Gateway routing.
- Authentication strategy.
- Cross-service permission checks.
- Event communication.
- Saga workflow.
- Caching.
- Failure handling.
- Local process orchestration.
- Angular structure.

### Step 3 — Repository foundation

Create:

- Root solution/repository conventions.
- AGENTS.md.
- Documentation.
- YARP gateway project.
- Angular workspace.
- Shared technical conventions.
- Initial test structure.

### Step 4 — First real microservice

Create **FindYourFood.Identity** with separate Clean Architecture projects.

Create:

```text
FindYourFood.Identity.Api
FindYourFood.Identity.Application
FindYourFood.Identity.Domain
FindYourFood.Identity.Infrastructure
FindYourFood.Identity.UnitTests
FindYourFood.Identity.IntegrationTests
```

Create its private SQL Server database:

`FindYourFood_IdentityDb`

Configure EF Core migrations, health checks, OpenAPI, validation, structured logging and secure authentication foundation.

### Step 5 — API Gateway integration

Route Identity's public endpoints through YARP.

Verify routing, service availability and appropriate authorization behavior.

### Step 6 — First Angular vertical slice

Implement the initial login/registration interface.

Connect it to the actual Identity API through the Gateway.

Add loading, validation, success and error states.

### Step 7 — Test thoroughly

Run:

- Backend build.
- Unit tests.
- SQL Server migration verification.
- Identity integration tests.
- Gateway routing tests.
- Angular build.
- Frontend unit tests.
- End-to-end identity flow where applicable.

Fix any failures.

### Step 8 — Commit and report

Commit the working foundation.

Push to GitHub if authorized.

Update progress and blocker documentation.

### Step 9 — Continue

Proceed to owner bootstrap and Organization Service permission hierarchy.

Then implement the remaining services incrementally according to the approved development roadmap.

Do not install Docker.

Do not force Kafka/RabbitMQ/Valkey to run before a relevant integration milestone.

Do not weaken service or database independence for convenience.

---

# FINAL NON-NEGOTIABLE PROJECT DIRECTIVES

**FindYourFood must use true microservices, not a monolith.**

**Every business microservice must use Clean Architecture.**

**Every business microservice must own its own private database.**

**Use an independent YARP API Gateway.**

**Plan reliable RabbitMQ integration for asynchronous operational messaging.**

**Plan Kafka for replayable high-volume events and analytics.**

**Use Redis-compatible distributed caching where appropriate.**

**Use OpenTelemetry for distributed observability.**

**Support intelligent nearby restaurant discovery.**

**Support traffic-aware rider dispatch when real traffic data is available.**

**Support dynamic backend-controlled Angular design.**

**Support festivals, customer-controlled food preferences and creative notifications.**

**Implement secure Owner → CEO → Manager → Employee authorization with scoped delegation and full auditability.**

**Support registered customers and secure guest checkout.**

**No Docker during current development.**

**Prefer free developer technologies and explicitly identify commercial limitations.**

**Build one backend/frontend vertical slice, test it, commit it and then continue.**

**Do not bypass permissions, fabricate test results or hide blockers.**

**Preserve the entire product roadmap and development progress in the repository.**

**Think like a principal engineer building a real food delivery company, not a developer creating a CRUD demonstration.**

The final goal is a scalable, secure, extensible, independently deployable food delivery ecosystem capable of growing from one local service area to a much larger commercial platform.

**START WITH THE ENVIRONMENT AUDIT, THEN IMPLEMENT THE FIRST REAL MICROSERVICE AND CONNECT IT TO ANGULAR THROUGH THE API GATEWAY.**
