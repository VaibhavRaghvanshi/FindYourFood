# FindYourFood

FindYourFood is a Windows-first development monorepo for independently deployed food-delivery microservices. The first slice connects the Angular customer app through YARP to the Identity service and its private SQL Server database.

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

## Verify

```powershell
$env:FINDYOURFOOD_TEST_SQL = 'Server=.\MSSQL22;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
.\scripts\Test-Services.ps1
```

SQL integration tests require database creation rights on the local instance and create a temporary, isolated database. For browser verification, start the integration environment, then set `FINDYOURFOOD_BROWSER` to `msedge` or `chrome` for an installed browser. Alternatively run `npx playwright install chromium` from the frontend folder. Run `.\scripts\Test-Services.ps1 -EndToEnd` to add browser and gateway checks. Browser registration creates a synthetic account in the development Identity database.

[Product requirements](docs/requirements/PRODUCT_REQUIREMENTS.md) · [Architecture](docs/architecture/SYSTEM_ARCHITECTURE.md) · [Progress](docs/PROGRESS.md) · [Blockers](docs/BLOCKERS.md)
