# FindYourFood

FindYourFood is a Windows-first development monorepo for independently deployed food-delivery microservices. The Angular customer app connects through YARP to Identity. The Angular staff portal also uses Organization to manage scoped permissions. Each service owns its SQL Server database.

See [API structure](docs/architecture/API_STRUCTURE.md) for the controller, repository, interface, and unit-of-work paths in each service.

## Run locally

Use PowerShell 7, Git, the stable .NET 10 SDK in [global.json](global.json), Node.js 24 LTS (24.15 or newer), and a local SQL Server instance. The scripts use workspace-local SDKs under `.local-tools/dotnet` and `.local-tools/node` when present. The default SQL instance is `.\MSSQL22`, using Windows authentication. To use another local instance, set `ConnectionStrings__Identity` with database name `FindYourFood_IdentityDb` before running the scripts.

```powershell
.\scripts\Check-Prerequisites.ps1
Push-Location frontend
npm ci
Pop-Location
.\scripts\Initialize-IdentityDatabase.ps1
.\scripts\Start-IntegrationEnvironment.ps1
```

Open [the customer app](http://localhost:4200). Use a synthetic account for the local register and sign-in flow. Gateway listens on localhost port 5100. Identity listens on 5101. Its [readiness probe](http://localhost:5101/health/ready) requires the migrated database. Its [OpenAPI document](http://localhost:5101/openapi/v1.json) is available directly in development.

The scripts generate a local signing key pair and write process logs under ignored `.local-state`. Loopback HTTP is the development default. For manual local HTTPS, trust the .NET development certificate with `dotnet dev-certs https --trust`, bind each host to the selected HTTPS URL, and update the gateway destination and Angular proxy. Public deployments require managed TLS.

Start the backend pair or frontend alone using `Start-IdentityEnvironment.ps1` or `Start-Frontend.ps1`. To run Identity and Gateway manually from two terminals:

```powershell
# Identity terminal, after applying the migration
. .\scripts\Development.ps1
Set-IdentityDevelopmentConfiguration
dotnet run --project $identityApi
```

```powershell
# Gateway terminal
. .\scripts\Development.ps1
$env:Tokens__PublicKeyPath = Join-Path $localState 'keys/identity-public.pem'
dotnet run --project $gateway
```

Stop tracked processes with `.\scripts\Stop-DevelopmentServices.ps1`. Terminate manually launched processes in their own terminal. RabbitMQ, Kafka, Valkey and OTLP collectors belong to later integration profiles.

## Run the staff portal

From an interactive PowerShell 7 terminal, apply the migrations, enroll the owner, and enroll any staff accounts:

```powershell
.\scripts\Stop-DevelopmentServices.ps1
.\scripts\Initialize-IdentityDatabase.ps1
.\scripts\Initialize-OrganizationDatabase.ps1
.\scripts\Enroll-PrivilegedAccount.ps1
.\scripts\Enroll-PrivilegedAccount.ps1 -Staff
.\scripts\Start-AdministrationEnvironment.ps1
```

Each enrollment prompts for a password and shows a time-based authenticator setup key once. Keep that key private. Enter the current code to complete enrollment, then wait for the next code before signing in. Run enrollment and Identity under the same Windows account so it can decrypt the local DPAPI key ring. Save the staff account UUID printed at enrollment to assign access in the portal. The database allows one owner enrollment.

Open [the staff portal](http://localhost:4201). Organization listens on port 5102 and the gateway routes staff requests on port 5100. To use another local SQL Server instance, set `ConnectionStrings__Organization` with database name `FindYourFood_OrganizationDb`. See [the Organization contract](docs/api-contracts/ORGANIZATION.md) for permission and delegation rules.

## Verify

```powershell
$env:FINDYOURFOOD_TEST_SQL = 'Server=.\MSSQL22;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
.\scripts\Test-Services.ps1
```

SQL integration tests require database creation rights on the local instance and create a temporary, isolated database. For browser verification, start the integration environment, then set `FINDYOURFOOD_BROWSER` to `msedge` or `chrome` for an installed browser. Alternatively run `npx playwright install chromium` from the frontend folder. Run `.\scripts\Test-Services.ps1 -EndToEnd` to add browser and gateway checks. Browser registration creates a synthetic account in the development Identity database.

For the staff browser test, stop the running profile and run `.\scripts\Test-AdministrationBrowser.ps1` with the same SQL and browser settings. It creates temporary databases and synthetic MFA accounts, exercises the staff portal through YARP, and cleans up the test profile.

[Product requirements](docs/requirements/PRODUCT_REQUIREMENTS.md) · [Architecture](docs/architecture/SYSTEM_ARCHITECTURE.md) · [Progress](docs/PROGRESS.md) · [Blockers](docs/BLOCKERS.md)
