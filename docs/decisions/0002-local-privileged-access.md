# ADR 0002 — Local privileged access

Use a native interactive command for owner and staff enrollment. It writes the Identity account and enrollment in one transaction after validating the current authenticator code. A filtered unique SQL index reserves one owner enrollment, and the user foreign key prevents accidental orphaning. Use Otp.NET for time-based codes and ASP.NET Data Protection with user-scoped Windows DPAPI for key encryption. The same Windows user runs enrollment and Identity. Persist the last accepted time step and atomically claim later steps to prevent replay.

Identity signs short-lived staff tokens after password and MFA verification. Organization checks these claims and manages scoped grants in its own database. Store grants and their administrative audit events together. Check complete delegation chains on every authorization decision. Public customer registration cannot create an MFA staff principal.

This local administration slice defers production OIDC, account recovery, staff invitations and profile lifecycle, policy integration into later business services, and production key management.
