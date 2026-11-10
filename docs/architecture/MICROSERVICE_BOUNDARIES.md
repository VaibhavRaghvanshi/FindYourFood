# Service boundaries

Identity, Organization, Experience and Provider have local development slices. The table records target capabilities and planned event contracts. Each gateway prefix is relative to `/api/v1/`.

| Service | Gateway prefix | Authoritative capability | Planned event examples | Degraded behavior |
| --- | --- | --- | --- | --- |
| Identity | `identity` | Accounts, credentials, sessions and security events | CustomerRegistered | SQL failure blocks auth and readiness |
| Organization | `organization` | Staff hierarchy, scoped permissions, grants and audit | PermissionChanged | Sensitive services deny on timeout |
| Customer | `customers` | Profiles, addresses, preferences, favorites and guest references | CustomerPreferencesChanged | Checkout uses approved existing address snapshots only |
| Provider | `providers` | Onboarding, documents, branches, approval and hours | ProviderApproved, RestaurantAvailabilityChanged | Existing projection remains visible until expiry |
| Catalog | `catalog` | Menus, variants, ingredients, allergens, prices and availability | MenuPublished | Block checkout price confirmation on timeout |
| Discovery | `discovery` | Search projection, cuisine and nearby ranking | — | Use bounded last-known public results |
| Location | `locations` | Geocoding, service zones, route and ETA adapters | ServiceAreaChanged | Manual address and marked geometric estimates |
| Cart | `carts` | Guest and signed-in carts, provisional pricing and coupons | — | Preserve cart, block final quote if a dependency fails |
| Ordering | `orders` | Authoritative order, snapshots, state machine and saga | OrderPlaced, OrderAccepted, OrderCancelled | Persist pending workflow for retry or review |
| Delivery | `deliveries` | Rider availability, atomic assignment and tracking | RiderAssigned, FoodPickedUp, OrderDelivered | Continue assignment retries or route for manual action |
| Payment | `payments` | Intents, provider transactions, webhooks and refunds | PaymentCaptured, PaymentFailed, RefundCompleted | Reconcile ambiguous responses before retry |
| Finance | `finance` | Journals, commissions, earnings, payouts and reconciliation | SettlementApproved | Queue reconciliation and hold payout |
| Notification | `notifications` | Templates, preferences, schedules and delivery attempts | NotificationDeliveryFailed | Retry and dead-letter bounded attempts |
| Experience | `experience` | Allowlisted layouts and themes, revisions and publishing | ExperiencePublished | Use shipped or last valid published configuration |
| Campaign | `campaigns` | Festival schedules, regional promotions and campaign rules | CampaignPublished | Use base experience and authoritative pricing |
| Support | `support` | Reviews, complaints, disputes and refund requests | SupportCaseEscalated | Keep durable requests for manual handling |

Discovery consumes Provider, Catalog and Location publications to build a searchable projection. Customer and Notification consume registration facts when their own workflows are implemented. Ordering consumes payment and delivery outcomes, Finance consumes captured payment, refund and delivered-order facts, and Notification consumes selected order and campaign facts. Campaign references Experience revisions and uses published Catalog and Provider references. Sensitive cross-service writes use authenticated HTTP policy decisions from Organization.

Audit records stay with the originating transaction first. An independent reporting projection may later consume curated audit and operational streams. The transactional services remain authoritative for authorization, finance and state transitions.
