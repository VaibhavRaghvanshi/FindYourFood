# Migration policy

Keep EF Core migrations and snapshots with the owning Infrastructure project. Generate with the corresponding API startup project and review the SQL. Apply through a controlled deploy-time identity. Service startup probes readiness and leaves schema changes to a separate migration operation. Prefer backward-compatible, additive changes followed by phased removal. Require an explicit backup and reviewed recovery plan for destructive changes. Keep migration history and backup restore tests per database.

The Identity migration creates ASP.NET Core Identity user tables and its unique normalized username index. Use the documented scripts for creation and schema verification.
