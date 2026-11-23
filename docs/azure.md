# Regneræven på Azure

Hjemmeside-udgaven (`src/Regneraeven.Server`) er den samme app som Windows-programmet, plus:

- **Forælder-konti** (e-mail + adgangskode). Hver forælder ser kun sine egne børns profiler.
- **Azure SQL** i stedet for en lokal SQLite-fil.
- **Invitationskode** (valgfri), så kun inviterede familier kan oprette en konto.

## Det billigste setup (ca. 0 kr./md.)

| Del | Valg | Pris |
|---|---|---|
| Hosting | Azure Container Apps, Consumption, skalerer til 0, 0,25 vCPU / 0,5 GiB | 0 kr. inden for den månedlige gratiskvote (180.000 vCPU-sek, 360.000 GiB-sek, 2 mio. forespørgsler) |
| Database | Azure SQL, gratis tilbud (serverless) | 0 kr. – 100.000 vCore-sek og 32 GB pr. måned. Når kvoten er brugt, **pauses** databasen til næste måned (den koster aldrig noget) |
| Container-image | GitHub Container Registry (bygges af GitHub Actions) | 0 kr. |
| Logning | Ingen Log Analytics | 0 kr. |

Ulempe ved det billige setup: den første side efter en pause tager et øjeblik (containeren starter på få sekunder; databasen kan bruge op til ca. et minut, hvis den har sovet). Appen prøver selv igen, mens databasen vågner.

> **Børns data:** Vælg en region i EU (fx `swedencentral` eller `northeurope`). Skal andre familier bruge siden, bør der også være en kort privatlivspolitik.

## Første gang

**1. Byg containeren.** Push til `main` – GitHub Actions (`.github/workflows/ci.yml`) tester koden og lægger billedet på `ghcr.io/mithione/regne-raeven:latest`.

**2. Lav et token, så Azure kan hente billedet.** Billedet er privat (ligesom repoet). Opret et *classic personal access token* på GitHub med kun `read:packages`.

**3. Opret alt i Azure** (kræver [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)):

```powershell
az login
az group create --name rg-regneraeven --location swedencentral
az deployment group create --resource-group rg-regneraeven --template-file infra/main.bicep `
  --parameters image=ghcr.io/mithione/regne-raeven:latest registryUsername=Mithione `
               registryPassword=<GITHUB-TOKEN> sqlAdminPassword=<STÆRK-ADGANGSKODE> inviteCode=<KODE>
```

Kommandoen skriver adressen (`url`) til sidst. Åbn den, opret en forælder-konto med invitationskoden, og opret børnenes profiler. Databasetabellerne oprettes automatisk ved første start.

**4. (Valgfrit) Eget domæne**, fx `regneraeven.dk`: Container Apps → *Custom domains* → tilføj domænet og et gratis administreret certifikat.

## Opdatere

Push til `main` → GitHub Actions bygger et nyt billede. Hent det i Azure:

```powershell
az containerapp update --name regneraeven --resource-group rg-regneraeven --image ghcr.io/mithione/regne-raeven:<commit-sha>
```

## Køre hjemmeside-udgaven lokalt

Kræver SQL Server LocalDB (følger med Visual Studio):

```powershell
dotnet run --project src/Regneraeven.Server --launch-profile local
```

Åbn http://localhost:5188 – invitationskoden lokalt er `raev`.

## Databaseændringer

Windows-programmet (SQLite) og hjemmesiden (SQL Server) har hver deres migreringer. Lav begge:

```powershell
dotnet ef migrations add <Navn> -p src/Regneraeven.Web -s src/Regneraeven.Desktop -o Data/Migrations
dotnet ef migrations add <Navn> -p src/Regneraeven.Server -s src/Regneraeven.Server --context AppDb -o Data/SqlServer/App
```

## Senere forbedringer

- Forbind til databasen med en *managed identity* i stedet for SQL-adgangskode.
- Automatisk udrulning fra GitHub Actions til Azure (OIDC), så `az containerapp update` ikke skal køres i hånden.
- "Glemt adgangskode" via e-mail (kræver en e-mail-tjeneste).
