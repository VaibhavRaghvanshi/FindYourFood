# Cart API

Cart listens on 5109 and owns `FindYourFood_CartDb`. It checks Provider and Catalog through public HTTP contracts and requests delivery quotes from Location.

| Method | Path | Behavior |
| --- | --- | --- |
| POST | `/api/v1/carts` | Create an empty branch cart. Supply branchId. |
| GET | `/api/v1/carts/mine` | Restore the latest active cart for the signed-in customer. |
| GET | `/api/v1/carts/{id}` | Read selections and current published prices. |
| PUT | `/api/v1/carts/{id}` | Replace selections with expectedRevision and lines. |
| DELETE | `/api/v1/carts/{id}` | Discard the owned cart. |
| POST | `/api/v1/carts/{id}/quote` | Revalidate prices, opening status and origin/destination coverage. |

Customer carts require their authenticated token subject. Guest creation returns a random 256-bit guestKey once. Subsequent guest requests use `X-Cart-Key`. SQL stores its SHA-256 hash. Other subjects and incorrect keys receive 404. Privileged staff accounts cannot use customer carts.

Carts expire after 24 hours and support 20 selections with quantities from 1 to 20. A selection contains lineId, itemId, quantity, variantId and unique addOnIds. Prices are integer paise and come from the current published menu. A required variant or unavailable item/option prevents checkout quotation. Cart totals are bounded to 100,000,000 paise. Stale writes and selections needing review return 409. Upstream outages return 503.

A delivery quote takes expectedRevision and a destination with latitude and longitude. Both the pickup and destination must lie inside an active Location zone, and the branch must be open with pickup coordinates configured. The response includes item and delivery totals, menu version, cart revision, destination and the check time. Checkout must obtain a fresh quote and compare the expected total.

The customer app provides option selection at `/menu/{branchId}` and cart review at `/cart`. Guest capability metadata is kept in tab session storage. JWTs stay in memory. Customer carts can also be restored through the authenticated API.

This slice quotes items and configured delivery fees. Tax policies, coupons, promotions and scheduled expiry cleanup remain separate roadmap work.
