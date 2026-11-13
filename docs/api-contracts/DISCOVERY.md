# Discovery API

Discovery listens on 5108 and owns `FindYourFood_DiscoveryDb`.

`POST /api/v1/discovery/search` accepts city, query, tag, openNow, page, latitude and longitude. City and page are required. Both coordinates are optional together. Search pages contain 20 results. Requests and responses use no-store and coordinates are passed in the body.

The city projection contains approved public Provider branches with at least one available item in a published Catalog menu. A city is refreshed through those HTTP APIs on the first query and after one minute. Refreshes replace the city's projection atomically and use optimistic concurrency. An incomplete refresh returns 503 and leaves the previous projection intact.

Search filters names, available foods, a case-insensitive cuisine or dietary tag and current opening hours in each branch's time zone. When customer and branch coordinates are present, Location evaluates all candidates in one bounded coverage matrix. Results rank serviceable branches first, then straight-line distance and opening status. Menu prices are validated again by Cart and Ordering.

The local refresh is bounded to 1000 branches per city and a 12-second fetch deadline. Exceeding those bounds returns 503. Large-scale asynchronous indexing, event ingestion and preference-based personalization remain roadmap work.

Provider exposes `GET /api/v1/providers/public/branches?city=Pune&page=1` and `GET /api/v1/providers/public/branches/{id}`. These contracts contain approved public branch information and exclude verification files, contact email and review history. Pickup coordinates are nullable for older branches.

Initialize the Provider coordinate migration and the Catalog, Location and Discovery databases before using `Start-MarketplaceEnvironment.ps1 -Services Discovery`. The customer search page is `/restaurants`.
