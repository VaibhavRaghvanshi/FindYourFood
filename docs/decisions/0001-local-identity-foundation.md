# ADR 0001 — Local Identity foundation

Accepted for the first Windows development slice.

Use independent Identity and YARP processes. EF Core and ASP.NET Core Identity provide persistent accounts in private SQL Server storage. Use an ephemeral, asymmetric access token for the local credential workflow. Keep the private signing key in ignored local state and issue tokens only in the Development environment. This exercises end-to-end authorization while production OIDC, verification, session persistence and MFA are implemented as separate milestones.

Use Angular 22 in a multi-application CLI workspace and route the first customer feature through a same-origin development proxy. Add the other portals when their first vertical slices exist. Start no brokers, cache servers or telemetry collectors for this slice.
