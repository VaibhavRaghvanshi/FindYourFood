# Progress

## November 10, 2026

Phase 0 found an empty project folder and no Git repository or remote. Initialized the repository on `codex/identity-foundation`. Windows 11 has about 8 GB RAM and 48 GB free on D: at the audit. The system .NET 10 SDK was a preview and its Node 22.15 was below Angular 22's minimum. Added stable .NET SDK 10.0.403 and Node 24.22.2 in ignored workspace-local tooling. Existing SQL Server Developer 2022 instance `.\MSSQL22` accepted Windows authentication and did not contain the Identity database before migration.

The foundation implements the Identity host, its Clean Architecture projects and private EF Core schema, YARP, shared hosting conventions, and the Angular customer account flow. The first JWT flow is development-only. Other services are specified in the architecture and planned for separate slices.

- Applied the initial migration to `FindYourFood_IdentityDb`. SQL confirmed the migration history and unique account index, and EF reported no pending model changes.
- Built the backend with zero warnings or errors. Passed 8 unit tests and 4 real SQL Server integration tests, including account lockout and concurrent duplicate registration.
- Built Angular and passed 4 frontend unit tests. Passed 2 Playwright checks in Microsoft Edge for the gateway route policy and the browser registration, sign-in, and sign-out flow through SQL Server.
- NuGet and npm audits found no known dependency vulnerabilities in the checked feeds.

## Administration slice

- Added one-time owner enrollment and interactive staff enrollment. Staff sign-in requires a password and a fresh authenticator code. Seeds are encrypted and each accepted code window is consumed atomically.
- Added the independent Organization service and `FindYourFood_OrganizationDb` migration. Scoped staff permissions support delegation, access checks, audit writes and revocation through the Angular admin portal and YARP.
- Passed 24 backend and 7 frontend unit and integration tests. The SQL tests cover owner uniqueness, MFA replay, scope and hierarchy enforcement, descendant revocation and audit records. Both Angular apps build independently.
- Passed the staff browser flow against isolated Identity and Organization databases through the live gateway, plus both customer browser regression tests. Applied both migrations to the regular local databases and confirmed there are no pending model changes.
- Published the foundation on the default GitHub branch. See [work plan](WORK_PLAN.md) for the remaining product scope.

## API folder layout

- Grouped each service's solution, API, Application, Domain, Infrastructure, and test projects under `backend/services/FindYourFood.<Service>.Api`.
- Moved the Identity and Organization routes into controllers and separated repository interfaces, EF Core repositories, and units of work. The owner enrollment and organization grant transactions use their own service DbContexts.
- Built each service solution independently. EF Core found no pending model changes. See [API structure](architecture/API_STRUCTURE.md) for direct code paths.

## Customer homepage Experience slice

- Added the independent Experience API with controllers, application services, repository and unit of work. Applied the migration to `FindYourFood_ExperienceDb` and confirmed no pending model changes.
- The staff portal can edit and preview a bounded homepage configuration, save a draft, approve it, publish a version and restore a prior publication. Each admin request checks Organization for the matching scoped permission.
- Customer Angular renders the published version at `/home` with a shared component registry, a last validated release cache and a bundled fallback.
- Both Angular apps built. Passed 29 backend and 10 frontend unit and integration tests. The backend tests covered concurrent saves, approval enforcement, rollback and permission outages.
- Passed the browser publishing and rollback flow through YARP, the staff portal, Organization, Experience and the customer homepage. The browser retained the restored release during a simulated read outage.

## Provider onboarding slice

- Added a separate Provider API, repository, and unit of work. Applied the initial migration to `FindYourFood_ProviderDb` for provider profiles, private verification files, branches, and review audit records. EF Core reported no pending model changes.
- The provider portal supports account creation, applications, document upload, resubmission, business settings, branch addresses, weekly hours and temporary closures. Staff can review applications through MFA and a live scoped Organization permission check.
- Used the supplied FindYourFood PNG as the shared frontend logo and favicon in the customer, staff, and provider apps. The built assets matched the source file's hash.
- Passed 34 backend and 12 frontend unit and integration tests, including SQL-backed verification and branch operations.
- Passed the browser flow for account creation, both document uploads, staff download and approval, and saving branch hours through YARP.

## Catalog menus

- Applied the initial migration to `FindYourFood_CatalogDb`. EF Core reported no pending model changes.
- Approved branch owners can edit menus and publish immutable versions. Customers see the last published version, including ingredients, dietary tags, allergens, variants and add-ons.
- Passed the Catalog unit test and SQL Server integration test for owner authorization, revision conflicts and publication visibility. All three Angular apps built.

## Customer profiles

- Applied the initial migration to `FindYourFood_CustomerDb`. EF Core reported no pending model changes.
- Customers can save profile settings, multiple delivery addresses, dietary preferences and favorite menus. Personalization defaults to off. The browser retains the access token in memory for the development session.
- Passed the Customer unit and SQL Server integration tests, including concurrent updates and cross-account access. All three Angular apps built and 12 frontend tests passed.

## November 13, 2026 — Location

- Applied the Location migration to `FindYourFood_LocationDb`. Zone administration requires the MFA owner account and uses revision checks.
- Added delivery coverage checking and an origin/destination quote requiring both points in one enabled delivery zone. The routing port currently uses an explicitly identified straight-line estimator.
- Built all three Angular apps. Passed Location unit and SQL integration tests for geographic boundaries, owner-only changes, stale updates, active coverage and cross-zone rejection. Road and traffic provider integration remains.

## Discovery

- Applied `FindYourFood_DiscoveryDb` and the Provider branch-coordinate migration. Both EF models had no pending changes.
- Added approved public branch contracts, atomic city projection refresh, food and tag filtering, opening-hour checks and Location coverage ranking. The customer app provides city and location fallback search at `/restaurants`.
- Passed nine tests across Provider, Location and Discovery, including SQL-backed projection refresh failures, cross-zone checks and case-insensitive city lookup. All three Angular apps built.
- Passed the combined Edge browser flow through YARP using isolated databases. It covered provider documents and approval, branch coordinates, menu publication, owner delivery areas, customer address persistence, restaurant search and favorite saving. The temporary databases were removed after the run.

## Cart and pricing

- Applied `FindYourFood_CartDb`. EF Core reported no pending model changes.
- Added customer carts and guest carts with a hashed 256-bit capability, revision checks, 24-hour expiry, current published prices, variant/add-on validation and fresh delivery quotes.
- Passed the Cart pricing unit test and SQL integration test, including cross-account access, concurrent updates, price changes, delivery rejection, restoration, discard and expiry. All three Angular apps built.
- Passed the expanded Edge browser flow for authenticated and guest carts, delivery totals and guest refresh persistence. Checked the mobile screenshot with the supplied logo and confirmed no horizontal overflow at 390 pixels. Temporary databases were removed.

## November 14, 2026 — Ordering checkpoint

- Applied `FindYourFood_OrderingDb`. EF Core reported no pending model changes, and SQL confirmed ten regular service databases online.
- Checkout obtains a fresh Cart quote and binds the confirmed total and delivery details to an immutable order snapshot. SQL enforces scoped idempotency and a unique cart/revision pair. Order, saga and pending outbox events commit together.
- Added private customer and guest history, status and cancellation. Unpaid orders expire after a configurable 15-minute default. The customer app provides checkout and order tracking, with guest access retained in tab session storage.
- Passed the Ordering unit and SQL integration tests for concurrent checkout, retries during an upstream outage, snapshot persistence, cross-account privacy, outbox counts, cancellation and expiry. Re-ran Cart tests after sharing the customer request guard. All three Angular apps built and 12 frontend tests passed.
- Passed the expanded Edge browser flow through SQL Server and YARP, including customer checkout/history/cancellation and guest checkout, refresh access and cancellation. Reviewed the mobile order screenshot and confirmed no horizontal overflow.
- Payment, delivery and broker-driven status transitions are the next integrations.

## November 14, 2026 — Payment and native messaging

- Applied `FindYourFood_PaymentDb` and the Ordering messaging migration. Eleven regular service databases are now migrated locally.
- Added a shared native RabbitMQ client with SQL outbox claims, broker publisher confirmations, manual consumer acknowledgements, per-service inbox hashes and durable rejected-message records. The broker uses pinned, hash-verified workspace-local tools and loopback credentials outside source control.
- Payment has its own controllers, application interfaces, repositories and unit of work. Attempts and refund instructions use stable server identifiers, revision checks and durable retry claims. Lost responses replay the same gateway operation. Signed development webhooks validate raw bytes, timestamp, amount, currency and event replay.
- Ordering advances to Placed from Payment events, retains declined attempts for retry before expiry, and requests refund compensation after cancellation or late capture. Customer and guest screens show server-confirmed payment and refund status. Simulation is explicitly development-only and transfers no money.
- Passed nine Ordering/Payment unit and SQL integration tests, including a declined guest attempt followed by capture and refund. All three Angular apps built and twelve frontend tests passed. Passed the expanded Edge browser flow for authenticated and guest payments, successful retry after decline, paid cancellation and refund. A separate SQL check verified two broker-confirmed payments and two completed refund sagas; the test databases and broker objects were cleaned up. Reviewed the mobile payment screenshot with the supplied logo.
- The browser profile uses built Angular assets on isolated ports and checks for occupied ports. Development API children use workstation GC unless explicitly overridden; application cleanup stops their complete process trees.
- Delivery, Finance, Notification, Campaign, Support, expanded existing-service features and production hardening remain. Live payment adapters and production credentials are not implemented by the development simulation.
