# Location API

Location listens on 5107 and owns `FindYourFood_LocationDb`.

- `POST /api/v1/location/serviceability` takes latitude and longitude in the body. It returns coverage and the configured INR delivery fee for the nearest enabled zone containing the point. Coordinates are validated and responses use no-store.
- `POST /api/v1/location/route-quote` takes origin and destination coordinates and requires both points inside the same active zone. It returns that zone's delivery fee and straight-line distance between the points.
- `POST /api/v1/location/coverage-matrix` takes one destination and up to 1000 origins. It returns corresponding delivery quotes using one zone lookup.
- `GET /api/v1/location/zones?page=1` returns 25 zones per page.
- `PUT /api/v1/location/zones/{id}` takes the current revision and settings. Revision zero creates a zone. A stale update returns 409.

Zone administration currently requires the MFA owner account. The staff portal provides editing, paging and activation. Customer coverage checking is at `/delivery-area` with manual coordinates and opt-in browser geolocation.

Areas use a center and a radius up to 80 km with Haversine distance. Empty databases have no configured coverage. The result identifies its estimate method. Road-distance, geocoding, traffic adapters and spatial indexing remain roadmap work.
