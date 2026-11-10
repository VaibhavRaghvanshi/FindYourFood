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
