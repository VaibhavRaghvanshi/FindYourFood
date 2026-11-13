# FindYourFood API structure

Each service is grouped like the reference SRECommerce services. Start in `backend/services/FindYourFood.<Service>.Api` and open that service's solution or its API project. The root [FindYourFood solution](../../FindYourFood.slnx) shows the same grouping.

| What to find | Folder inside the service |
| --- | --- |
| HTTP methods, routes and authorization attributes | `FindYourFood.<Service>.Api/Controllers` |
| Request and response contracts | `FindYourFood.<Service>.Application/DTOs` |
| Business workflows | `FindYourFood.<Service>.Application/Services` |
| Repository and transaction contracts | `FindYourFood.<Service>.Application/Interfaces` |
| Entities, rules and value objects | `FindYourFood.<Service>.Domain` |
| EF Core queries and persistence adapters | `FindYourFood.<Service>.Infrastructure/Repositories` |
| SQL transaction and commit implementations | `FindYourFood.<Service>.Infrastructure/UnitOfWork` |
| DbContext, migrations and dependency registration | `FindYourFood.<Service>.Infrastructure/Data`, `Migrations`, and `DependencyInjection.cs` |
| Service tests | `FindYourFood.<Service>.UnitTests` and `FindYourFood.<Service>.IntegrationTests` |

- **Identity.** Start with [customer accounts](../../backend/services/FindYourFood.Identity.Api/FindYourFood.Identity.Api/Controllers/CustomerAccountsController.cs) or [staff authentication](../../backend/services/FindYourFood.Identity.Api/FindYourFood.Identity.Api/Controllers/StaffAuthenticationController.cs). The [Identity repositories](../../backend/services/FindYourFood.Identity.Api/FindYourFood.Identity.Infrastructure/Repositories) use ASP.NET Core Identity. [IdentityUnitOfWork](../../backend/services/FindYourFood.Identity.Api/FindYourFood.Identity.Infrastructure/UnitOfWork/IdentityUnitOfWork.cs) keeps the user and privileged enrollment writes in the same transaction.
- **Organization.** Start with [grants](../../backend/services/FindYourFood.Organization.Api/FindYourFood.Organization.Api/Controllers/GrantsController.cs) or [permission checks](../../backend/services/FindYourFood.Organization.Api/FindYourFood.Organization.Api/Controllers/PermissionsController.cs). The [grant repository](../../backend/services/FindYourFood.Organization.Api/FindYourFood.Organization.Infrastructure/Repositories/GrantRepository.cs) uses the same scoped DbContext as [OrganizationUnitOfWork](../../backend/services/FindYourFood.Organization.Api/FindYourFood.Organization.Infrastructure/UnitOfWork/OrganizationUnitOfWork.cs), so grant changes and their audit rows commit together.
- **Gateway.** The [YARP project](../../backend/gateway/FindYourFood.ApiGateway) owns routing and gateway policies. Service controllers own HTTP behavior and call their Application services.
- **Catalog.** The [menus controller](../../backend/services/FindYourFood.Catalog.Api/FindYourFood.Catalog.Api/Controllers/MenusController.cs) handles branch drafts and publications.
- **Customer.** The [profiles controller](../../backend/services/FindYourFood.Customer.Api/FindYourFood.Customer.Api/Controllers/CustomerProfilesController.cs) handles private customer settings and addresses.
- **Location.** The [location controller](../../backend/services/FindYourFood.Location.Api/FindYourFood.Location.Api/Controllers/LocationController.cs) handles areas, coverage and delivery quotes.
- **Discovery.** The [search controller](../../backend/services/FindYourFood.Discovery.Api/FindYourFood.Discovery.Api/Controllers/RestaurantSearchController.cs) handles published restaurant and food search.
- **Cart.** The [carts controller](../../backend/services/FindYourFood.Cart.Api/FindYourFood.Cart.Api/Controllers/CartsController.cs) handles ownership, selections and current pricing.
- **Experience.** Start with the [public homepage controller](../../backend/services/FindYourFood.Experience.Api/FindYourFood.Experience.Api/Controllers/PublicExperienceController.cs) or the [staff publishing controller](../../backend/services/FindYourFood.Experience.Api/FindYourFood.Experience.Api/Controllers/ExperienceAdministrationController.cs). [ExperiencePublishing](../../backend/services/FindYourFood.Experience.Api/FindYourFood.Experience.Application/Services/ExperiencePublishing.cs) owns workflow decisions and uses the service's own repository and unit of work.
- **Provider.** Start with the [applications controller](../../backend/services/FindYourFood.Provider.Api/FindYourFood.Provider.Api/Controllers/ProviderApplicationsController.cs) or the [staff reviews controller](../../backend/services/FindYourFood.Provider.Api/FindYourFood.Provider.Api/Controllers/ProviderReviewsController.cs). [ProviderOnboarding](../../backend/services/FindYourFood.Provider.Api/FindYourFood.Provider.Application/Services/ProviderOnboarding.cs) handles revisioned changes through the provider repository and unit of work.
