# Authorization

Organization owns scope, membership, delegation and revocation. Identity authenticates the subject. The gateway filters public endpoints, while each service checks actor, action, resource, scope and attributes. Build policies around allowlisted actions and immutable subject identifiers.

For high-risk writes, fetch a fresh policy decision over authenticated service-to-service transport and fail closed if unavailable. For approved low-risk reads, use short-lived decisions keyed by subject, action, resource and policy version. Invalidate on permission changes. Record decisions and mutations with actor, action, scope, timestamp and correlation ID. Keep raw credentials and financial data out of audit details.

Customer registration grants no organization membership. The implemented `/me` reads only the customer subject from a validated token. The local Organization service requires staff MFA and checks live SQL-backed grant chains on each permission decision. See [the Organization API contract](../api-contracts/ORGANIZATION.md).
