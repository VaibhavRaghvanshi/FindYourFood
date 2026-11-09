# Authentication

Identity is the credential authority. The initial local flow uses ASP.NET Core Identity password hashing, normalized account identifiers, unique SQL index, lockout and input bounds. New email addresses are unverified. For this development slice, Identity issues a short-lived RS256 JWT and both hosts verify algorithm, signature, token type, issuer, audience and expiration. The gateway requires only the public key. The Angular feature uses the token in memory for the profile request. API responses with token or profile data use `Cache-Control: no-store`.

Local owner and staff enrollment runs in an interactive terminal and requires a valid authenticator code. Identity encrypts the TOTP seed with ASP.NET Data Protection and user-scoped Windows DPAPI. A unique owner enrollment, atomic code consumption, password verification and lockout guard staff sign-in. Organization accepts staff tokens only after MFA. Run enrollment and the local Identity host under the same Windows user.

Before customer rollout, add an OIDC Authorization Code flow with PKCE and protected browser sessions, email verification, password recovery, rotated refresh token families with reuse detection, revocation, asymmetric key rotation with JWKS, account lifecycle and security audit. Treat guest tokens as checkout-scoped capabilities. Use distinct workload credentials for internal calls and a production enrollment and recovery ceremony for privileged users.

Keep token keys outside source control and backups with appropriate access. Local HTTP binds to loopback for synthetic credentials. Public and internal deployments require managed TLS and a production identity provider.
