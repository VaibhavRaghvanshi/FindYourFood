# Customer API

Customer listens on port 5106 and owns `FindYourFood_CustomerDb`. It does not store credentials.

`GET /api/v1/customer/me` requires a customer access token and returns the profile for the token subject. New profiles have revision zero and empty settings. `PUT /api/v1/customer/me` accepts the current revision and complete settings, and returns the saved revision. Concurrent or stale changes return 409. The server takes the customer ID from the token and rejects privileged staff accounts. Responses are private and marked no-store.

Settings include a display name, up to eight delivery addresses, a default address, dietary preferences, favorite branch IDs, and a personalization opt-in. Guest shopping will use the Cart flow.
