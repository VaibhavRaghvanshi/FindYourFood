# Database catalog

The named local instance is `.\MSSQL22` on this machine. Ten of the sixteen planned service databases have applied local migrations. Each migration belongs to its service's Infrastructure project.

| Database | Local migration status |
| --- | --- |
| `FindYourFood_IdentityDb` | Applied |
| `FindYourFood_OrganizationDb` | Applied |
| `FindYourFood_CustomerDb` | Applied |
| `FindYourFood_ProviderDb` | Applied |
| `FindYourFood_CatalogDb` | Applied |
| `FindYourFood_DiscoveryDb` | Applied |
| `FindYourFood_LocationDb` | Applied |
| `FindYourFood_CartDb` | Applied |
| `FindYourFood_OrderingDb` | Applied |
| `FindYourFood_DeliveryDb` | Planned |
| `FindYourFood_PaymentDb` | Planned |
| `FindYourFood_FinanceDb` | Planned |
| `FindYourFood_NotificationDb` | Planned |
| `FindYourFood_ExperienceDb` | Applied |
| `FindYourFood_CampaignDb` | Planned |
| `FindYourFood_SupportDb` | Planned |

Integration tests create and remove uniquely named `FindYourFood_<Service>Test_*` databases. Use a local test SQL connection with database creation rights. A deployed service uses a separate runtime principal with privileges limited to its owned schema.
