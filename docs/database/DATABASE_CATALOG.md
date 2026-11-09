# Database catalog

The named local instance is `.\MSSQL22` on this machine. Each database below is reserved for its owner. Identity and Organization have migrations in their respective Infrastructure projects.

| Database | Owner |
| --- | --- |
| `FindYourFood_IdentityDb` | Identity |
| `FindYourFood_OrganizationDb` | Organization |
| `FindYourFood_CustomerDb` | Customer |
| `FindYourFood_ProviderDb` | Provider |
| `FindYourFood_CatalogDb` | Catalog |
| `FindYourFood_DiscoveryDb` | Discovery |
| `FindYourFood_LocationDb` | Location |
| `FindYourFood_CartDb` | Cart |
| `FindYourFood_OrderingDb` | Ordering |
| `FindYourFood_DeliveryDb` | Delivery |
| `FindYourFood_PaymentDb` | Payment |
| `FindYourFood_FinanceDb` | Finance |
| `FindYourFood_NotificationDb` | Notification |
| `FindYourFood_ExperienceDb` | Experience |
| `FindYourFood_CampaignDb` | Campaign |
| `FindYourFood_SupportDb` | Support |

Integration tests create and remove uniquely named `FindYourFood_IdentityTest_*` and `FindYourFood_OrganizationTest_*` databases. Use a local test SQL connection with database creation rights. A deployed service uses a separate runtime principal with privileges limited to its owned schema.
