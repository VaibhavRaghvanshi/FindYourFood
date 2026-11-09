# Test strategy

Use xUnit for domain and application rules and ASP.NET Core test hosting with real SQL Server for Identity integration tests. The SQL fixture migrates an isolated database and checks readiness, OpenAPI, duplicate registration, lockout and authenticated profile access. Use Angular TestBed for form and HTTP behavior. Playwright drives a real browser through Angular, the development proxy, YARP, Identity and SQL Server.

Add API contract and version-compatibility checks for cross-service interactions. Exercise authorization escalation, payment idempotency and saga recovery before rollout. Add dedicated native integration profiles for broker and cache failure/replay and use k6 for measured scenarios. Record test counts, required dependencies and pending external verification.
