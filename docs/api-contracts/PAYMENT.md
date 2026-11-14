# Payment and native messaging

Payment owns `FindYourFood_PaymentDb` on port 5111. Controllers, application ports, EF repositories and the unit of work follow the same service folder structure as Ordering. No service reads another service's database.

## HTTP

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/v1/payments/orders/{orderId}` | The signed-in customer or the original guest's `X-Cart-Key` |
| POST | `/api/v1/payments/orders/{orderId}/simulate` | Same ownership; development only |
| POST | `/api/v1/payments/webhooks/development` | HMAC signature; development only |

Simulation accepts `idempotencyKey` (UUID), `expectedTotalMinor` (integer paise), and `success` (boolean). The development adapter creates stable server transaction references. It accepts no card details and transfers no money. Production provider adapters and their own signed-webhook formats remain separate work. The simulation endpoint returns 404 outside Development.

An attempt is committed before contacting the gateway. Its UUID is the gateway idempotency identifier. A durable 30-second claim prevents concurrent work. Lost responses leave an attempt pending; the worker retries the same identifier, so adapters must reconcile or replay the original gateway result. A declined attempt permits another UUID before the order's payment deadline. A fresh attempt checks Ordering through HTTP with the customer's existing credentials.

Never interpret a browser response as proof that an order is paid. Only the committed Payment outbox and Ordering inbox advance the saga. Amount and currency must match the immutable order snapshot. A late capture on a cancelled order requests a refund. Refund instructions are unique per payment, claimed durably and executed with stable refund UUIDs.

## Development webhook

Set `Payment__WebhookKeyPath` to a private file of at least 32 random bytes. Send `X-Payment-Timestamp` as Unix seconds and `X-Payment-Signature` as the hexadecimal HMAC-SHA256 of UTF-8 timestamp, a period and the exact raw request bytes. The body is `{eventId,paymentId,totalMinor,currency,success,reference}`. Events outside a five-minute window or with altered bytes fail authentication. Event IDs are deduplicated with a content hash in the same transaction as payment completion.

## Events

Payment consumes `ordering.payment-requested.v1`, `ordering.payment-cancelled.v1`, and `ordering.refund-requested.v1`. It publishes `payment.succeeded.v1`, `payment.failed.v1`, and `payment.refunded.v1`. The envelope includes UUID, source, type, aggregate UUID/version, occurrence time and an object payload.

The shared SQL outbox leases work, retries outages, awaits broker publisher confirmations and only then marks publication. The consumer uses durable queues and manual acknowledgement after its database commit. A crash between commit and acknowledgement replays safely through the inbox. Invalid envelopes are stored in the receiving service's rejected-message table before acknowledgement. These technical tables remain private to each service.

Messages without a bound consumer remain in the originating SQL outbox; they are retried. Adding a later consumer does not silently discard work. Production broker credentials and queue permissions, dead-letter operational tooling, retention and recovery dashboards still need deployment configuration.

## Native Windows profile

```powershell
.\scripts\Initialize-MessagingTools.ps1
.\scripts\Initialize-MarketplaceDatabase.ps1 -Service Payment
.\scripts\Start-MarketplaceEnvironment.ps1 -Services Discovery,Customer,Payment
```

This installs pinned, hash-verified workspace-local Erlang, RabbitMQ and 7-Zip under ignored `.local-tools`. It starts only a loopback broker, with generated credentials under ignored `.local-state/messaging`. No Docker or Windows service installation is used.

```powershell
$env:FINDYOURFOOD_TEST_SQL = 'Server=.\MSSQL22;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:FINDYOURFOOD_BROWSER = 'msedge'
.\scripts\Test-AdministrationBrowser.ps1 -Payments
```

The Payments browser profile builds and serves Angular assets on ports 4300–4302, uses isolated service databases and generates a RabbitMQ queue/exchange prefix. Set `FINDYOURFOOD_E2E_PORT_OFFSET` to change the default offset of 100 from development ports. Use `-SkipBuild` only after building the current backend and all three frontend apps.

It checks authenticated and guest payments, a declined attempt followed by success, cancellation and refunds, then verifies broker publication and completed Ordering sagas. Cleanup targets only the generated test databases and broker names. Stop application processes with `Stop-DevelopmentServices.ps1`, and stop the workspace broker with `Stop-MessagingBroker.ps1`.

Local development API processes default to workstation garbage collection, while an explicit `DOTNET_gcServer` value is preserved. This is scoped to child processes started by the development helper; deployment runtime configuration remains independent. See the [.NET runtime settings](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector) and [environment precedence](https://learn.microsoft.com/en-us/dotnet/core/compatibility/deployment/9.0/envvar-precedence).
