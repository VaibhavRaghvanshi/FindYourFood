# Catalog API

Catalog listens on port 5105 in the Windows marketplace profile. Its private SQL Server database is `FindYourFood_CatalogDb`. Prices and option adjustments use nonnegative integer paise (INR).

| Method | Path under `/api/v1/catalog/branches/{branchId}` | Access |
| --- | --- | --- |
| GET | base path | Public latest publication. Returns 404 before publication. |
| GET | `/workspace` | Approved provider owner of this branch |
| PUT | `/draft` | Approved branch owner. Send the current revision and complete menu content. |
| POST | `/publish` | Approved branch owner. Send the current revision. |

Draft and publication writes return 409 if another save changed the revision. Catalog confirms branch ownership against Provider on each protected call. A Provider outage returns 503. A draft edit leaves the current public version available until publication. Catalog keeps an immutable copy of each published version. Menu editing supports category, availability, ingredients, dietary tags, allergens, variants, and add-ons.
