# Organization API v1

The local Administration profile exposes Organization on port 5102 and routes the listed paths through YARP on port 5100. Both hops require a validated token from the Identity staff login with `amr=mfa` and `account_type=staff` or `owner`.

| Method and path | Response |
| --- | --- |
| `GET /api/v1/organization/grants?page=0` | 200, up to 50 visible grants. Owners see all grants. Staff see grants assigned to or issued by them. |
| `POST /api/v1/organization/grants` | 201 with the grant, 400 invalid input, 403 insufficient authority, 409 exact unrevoked assignment already exists |
| `DELETE /api/v1/organization/grants/{id}` | 204 for a permitted, idempotent revoke |
| `GET /api/v1/organization/permissions/check?permission=provider.review&scope=/city/pune` | 200 with an `allowed` boolean for the token subject |

The create request has `subjectId` from the enrollment command, `level` (Ceo, DepartmentHead, CityManager, TeamManager or Employee), `permission`, canonical `scope`, `canDelegate` and optional `parentGrantId`. Owners leave the parent empty. Delegation requires an active parent held by the actor and narrows or retains its scope. Child level must be lower and permission must match. A child cannot confer access on an ancestor or on the caller.

A revoke leaves the historical grant and audit records in SQL. Descendants are immediately ineffective through the parent chain, even when the listed descendant still shows its original issue state. If replacing an exact descendant assignment, the owner can first revoke its historical row directly. Write contention can return 503. Sensitive consumers must treat all failure responses as denial and may not cache allow decisions across a revocation.
