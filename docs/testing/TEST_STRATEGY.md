# Test strategy

Use xUnit for domain and application rules and ASP.NET Core test hosting with real SQL Server for Identity integration tests. The SQL fixture migrates an isolated database and checks readiness, OpenAPI, duplicate registration, lockout and authenticated profile access. Use Angular TestBed for form and HTTP behavior. Playwright drives a real browser through Angular, the development proxy, YARP, Identity and SQL Server.

Organization integration tests verify staff MFA claims, scoped delegation, escalation denials, ancestor revocation and audit history against an isolated SQL database. The administration browser profile creates synthetic MFA accounts and checks the owner, manager and employee flow through both live services and YARP.

Add API contract and version-compatibility checks for cross-service interactions. Exercise authorization escalation, payment idempotency and saga recovery before rollout. Add dedicated native integration profiles for broker and cache failure/replay and use k6 for measured scenarios. Record test counts, required dependencies and pending external verification.
