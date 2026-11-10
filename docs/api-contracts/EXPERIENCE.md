# Experience API v1

Experience owns `FindYourFood_ExperienceDb`. Its local host listens on port 5103. The Experience profile exposes the following routes through YARP on port 5100.

| Method and route | Behavior |
| --- | --- |
| `GET /api/v1/experience/customer-home` | Public. Returns `version` and `configuration`, or 204 while the bundled homepage is live. |
| `GET /api/v1/experience/admin/customer-home` | Returns the draft, workflow state, current workspace revision, live version and 20 most recent releases. |
| `POST …/admin/customer-home/preview` | Validates and returns the proposed configuration without writing it. |
| `PUT …/admin/customer-home/draft` | Saves a validated configuration and clears prior approval. |
| `POST …/admin/customer-home/approve` | Approves the saved draft. |
| `POST …/admin/customer-home/publish` | Creates a new live release from the approved draft. |
| `POST …/admin/customer-home/rollback` | Restores an earlier release as a new version and records the source version. |

Each write sends `expectedRevision`. Save and preview also take `configuration`, and rollback takes the target `version`. Stale revisions and unavailable workflow transitions return 409. An unknown release returns 404. Invalid configuration returns 400.

Experience validates staff MFA claims and checks Organization before each admin request. Grants use scope `/experience/customer-home`. Workspace and preview accept either `experience.edit` or `experience.publish`. Saving requires edit. Approval, publishing and rollback require publish. If Organization is unavailable, Experience denies the operation and returns 503. An owner may perform both steps, and organizations can delegate the two permissions separately.

## Customer homepage schema

```json
{
  "schemaVersion": 1,
  "brand": "FindYourFood",
  "palette": "forest",
  "typography": "editorial",
  "showNavigation": true,
  "navigation": [{ "target": "account", "label": "My account" }],
  "sections": [{ "kind": "hero", "title": "Good food. Close by.", "body": "Your next favorite meal starts here." }]
}
```

The palette values are `forest`, `plum`, and `ocean`. Typography is `editorial` or `system`. Navigation targets are `home` and `account`, with one entry per target. Sections render in supplied order and require exactly one `hero`. Optional `banner` and `note` sections may each appear once. Labels and copy are bounded plain text rendered through Angular interpolation.

Release rows are immutable. Publishing and restoring invalidate the process-local cache, which otherwise expires after 30 seconds. The customer browser retains its last validated public release and uses the bundled configuration when it has no valid cached release. The implementation covers the shared customer homepage. City, role and festival variants, managed logo assets, and live restaurant collections depend on later slices.
