# Identity API v1

Development only. Send JSON with `email` and `password` to either credential route. Use the same gateway origin as the application.

| Method and path | Response |
| --- | --- |
| `POST /api/v1/identity/register` | 201 on creation, 400 invalid input, 409 duplicate |
| `POST /api/v1/identity/login` | 200 with `accessToken`, `tokenType` and `expiresIn` (seconds), 401 for failed sign-in or lockout |
| `POST /api/v1/identity/staff/login` | Also takes a six-digit `code`. Returns 200 with a staff or owner token after password and MFA verification, or 401 for failed sign-in. |
| `GET /api/v1/identity/me` | 200 with `id`, `email` and `emailVerified`, 401 if bearer token or user is invalid |

Password registration limits are 12–128 characters. Email identifiers are case-insensitive. Credential requests may return 429 under rate limiting. Protected responses prevent caching. Identity exposes OpenAPI at `/openapi/v1.json`, liveness at `/health/live` and SQL readiness at `/health/ready` on the service port. Health and OpenAPI are outside the gateway route allowlist.

Staff enrollment runs interactively on the host. Staff tokens carry `amr=mfa` and `account_type=staff` or `owner`. The customer login and profile endpoints are reserved for customer accounts.
