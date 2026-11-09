# Authentication

Identity is the credential authority. The initial local flow uses ASP.NET Core Identity password hashing, normalized account identifiers, unique SQL index, lockout and input bounds. New email addresses are unverified. For this development slice, Identity issues a short-lived RS256 JWT and both hosts verify algorithm, signature, token type, issuer, audience and expiration. The gateway requires only the public key. The Angular feature uses the token in memory for the profile request. API responses with token or profile data use `Cache-Control: no-store`.

Before customer rollout, add an OIDC Authorization Code flow with PKCE and protected browser sessions, email verification, password recovery, rotated refresh token families with reuse detection, revocation, asymmetric key rotation with JWKS, account lifecycle and security audit. Require MFA for privileged actions. Treat guest tokens as checkout-scoped capabilities. Use distinct workload credentials for internal calls. Protect the owner bootstrap with an audited, expiring one-time mechanism, and require MFA enrollment before owner privileges can be exercised.

Keep token keys outside source control and backups with appropriate access. Local HTTP binds to loopback for synthetic credentials. Public and internal deployments require managed TLS and a production identity provider.
