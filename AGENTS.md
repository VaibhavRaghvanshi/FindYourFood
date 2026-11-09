# FindYourFood

Read `docs/PROGRESS.md`, `docs/BLOCKERS.md`, `docs/WORK_PLAN.md`, and the relevant architecture and requirements before changing a feature.

- Build one end-to-end slice at a time. Pair the service contract, database migration, Angular flow, and verification before committing.
- Every business service has an independent ASP.NET Core host and private SQL Server database. Keep Domain independent, Application dependent on Domain, and Infrastructure behind Application ports. API composes the layers.
- Follow the [API structure](docs/architecture/API_STRUCTURE.md). Put each service's solution and projects side by side in `backend/services/FindYourFood.<Service>.Api`. Keep HTTP routes in `Api/Controllers`, ports in `Application/Interfaces`, and EF Core repositories and units of work in their respective Infrastructure folders.
- Exchange data across services through contracts. Never access another service's database or reference its implementation.
- Keep Windows development native and start only services needed for the current slice. Gate optional dependencies behind their integration profiles.
- Keep passwords, tokens, signing keys, and connection credentials out of source and logs. Record unverified dependencies and pending security milestones.
- Run the applicable tests, update progress, and commit verified work. Use the authorized GitHub workflow when a remote is available.
