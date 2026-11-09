# Data ownership

Every service has a private SQL Server database. An instance can be shared in local development. Separate production identities, grants, backup policies and migration accounts per database. See [Database catalog](../database/DATABASE_CATALOG.md).

Service relationships use stable identifiers and API or event contracts. Identity owns credentials, password hashes and subject IDs. Organization owns authority and delegations. Catalog owns prices and availability. Ordering snapshots confirmed prices, addresses and item descriptions into the order. Finance owns journals and settlements. Discovery is authoritative for its search projection only and is rebuilt from source events. Avoid cross-database queries, joins and foreign keys.

Use schema constraints and transactions for concurrency. Keep secrets, passwords and full payment details out of events, caches and logs. Define redaction, erasure and retention rules before onboarding live users.
