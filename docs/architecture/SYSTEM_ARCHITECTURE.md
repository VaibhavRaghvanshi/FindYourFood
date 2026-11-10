# System architecture

Customer, provider, rider and admin standalone Angular applications send public traffic to an independent YARP gateway. It routes versioned, explicitly mapped paths to individual ASP.NET Core hosts. Each business service uses its own Clean Architecture layers, SQL Server database, health probes, migrations, tests and deployment settings. The monorepo coordinates code and contracts. A service can build, migrate and run independently.

YARP exposes the customer and staff Identity endpoints. The Administration profile enables Organization grant, revoke and permission-check routes. The Experience profile adds the public homepage and staff publishing routes. The Provider profile exposes authenticated applications, uploads, review, and branch operations. YARP actively checks service readiness and applies route-level authentication, request limits and sign-in rate limits. Each service validates protected requests independently. Live probes reflect the process and readiness checks the dependencies needed to serve traffic.

In Windows development, launch the gateway and one profile at a time. Identity, Organization, Experience and Provider use native SQL Server. Localhost ports are 5100 for Gateway, 5101 for Identity, 5102 for Organization, 5103 for Experience, 5104 for Provider, 4200 for customer Angular, 4201 for staff Angular and 4202 for provider Angular. Later profiles include Catalog, Discovery/Location, Cart/Ordering/Payment and Dispatch/Delivery. Assign their ports when the corresponding host ships.

Angular projects live in `frontend/apps`. Each app owns lazy route groups and application state. Experience publishes versioned theme tokens and declarative layouts from an approved component registry. The customer page and staff preview share the renderer in `frontend/shared/experience`. The customer browser retains its last valid publication and includes a bundled fallback. Campaigns will select configurations by locale, geography, dietary preferences, priority and timezone.

Gateway and services use structured JSON logs, W3C trace context, correlation IDs and OpenTelemetry ASP.NET Core and HTTP instrumentation. OTLP export is opt-in for development. Dedicated profiles can send traces to Jaeger and metrics to a Prometheus-compatible collector and Grafana. Use bounded queues, timeout budgets and deployment-specific SLOs. Establish capacity through measured k6 scenarios.

## Planned customer and delivery flows

Discovery uses the customer's selected address or permission-based location. Filter verified, open and serviceable branches in a bounded SQL Server spatial query before ranking by availability, preparation time, ETA and explicit customer preferences. Fetch road estimates for a bounded candidate set. Expose data freshness and revalidate serviceability and price at checkout.

Dispatch filters approved riders by availability, capacity, zone and location freshness, then ranks eligible riders by road pickup ETA, preparation readiness, workload and reliability. Offers expire. Use a conditional SQL transaction for final acceptance so two riders cannot receive the same active assignment. A routing adapter can return traffic-aware estimates with source and freshness. Label geometric or stale estimates and allow manual dispatch. Use scoped SignalR groups for authorized viewers and a REST fallback for reconnects.

Provider owns onboarding and branch approval, while Catalog owns published menus and food labels. Project approved data into Discovery. Customer dietary modes are opt-in. Campaign scheduling uses explicit time zones, bounded priority, approval and calendar overrides. Keep transactional notifications distinct from consent-based marketing and enforce quiet hours, caps and deduplication in Notification.

Ordering supports guest and authenticated checkout with server-confirmed pricing. Payment separates authorization, capture, refund and reconciliation and verifies provider webhooks. Finance books balanced journal entries from settled facts and applies an approval workflow before initiating payouts. The first payment slice uses a development simulator.
