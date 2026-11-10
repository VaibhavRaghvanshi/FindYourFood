# Provider API v1

Provider owns `FindYourFood_ProviderDb` and runs on port 5104 in the local Provider profile. YARP routes its API from port 5100. An applicant signs in through Identity and owns one business profile through the token subject. Every change after creation sends the last `expectedRevision`. Stale revisions or unavailable transitions return 409.

| Route | Purpose |
| --- | --- |
| `GET, POST, PUT /api/v1/providers/me` | Read, register or update the caller's business. A first registration returns 201. An approved provider may update contact and description while the reviewed name and type stay fixed. |
| `PUT /api/v1/providers/me/documents/{kind}` | Upload a private multipart `document` with `expectedRevision`. Accepts PDF, PNG or JPEG signatures up to 2 MB. |
| `GET /api/v1/providers/me/documents/{kind}` | Download the caller's private document as an attachment. |
| `POST /api/v1/providers/me/submit` | Submit or resubmit a draft with both `license` and `address-proof` attached. |
| `POST /api/v1/providers/me/branches` | Create a branch after approval. |
| `PUT /api/v1/providers/me/branches/{id}` | Edit a branch address, time zone, weekly schedule or temporary closure. |
| `GET /api/v1/providers/admin` | List up to 50 pending applications. |
| `GET /api/v1/providers/admin/{id}` | Read the application and review history. |
| `GET /api/v1/providers/admin/{id}/documents/{kind}` | Download a submitted or approved application's document. |
| `POST /api/v1/providers/admin/{id}/review` | Approve or reject a submitted application. Rejecting requires a review note. |

Staff routes require MFA and a live Organization decision for `provider.review`. The queue requires scope `/providers`. For one application, grant `/providers/{id}` using the 32 lowercase hexadecimal digits of the provider ID without hyphens. The reviewer opens the application with its displayed ID. Reviewers cannot decide their own applications. Decisions are written as audit records in the same transaction as the status change.

Branch weekdays use 1 for Monday through 7 for Sunday. Each open day has one same-day interval with `opens` and `closes` in `HH:mm`. Omitted days are closed. `closedUntil` is a timestamp that temporarily overrides the schedule. Clearing it returns the branch to normal hours. A profile supports up to 20 branches.

This local slice stores bounded verification files in SQL Server and supports human review. Production file scanning and retention policies, provider personnel, and catalog publishing remain separate work.
