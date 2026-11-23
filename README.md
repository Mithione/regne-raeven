# Regneræven 🦊

Et Windows-program, der hjælper børn i **5.–6. klasse** med matematik. Det har regneregler, tabeller, trin-for-trin-hjælp, gemte opgaver med tags og et regnespil med daglige opgaver, XP, levels og rævetitler.

Programmet er bygget i .NET 8. Brugerfladen er en web-UI, der vises i et almindeligt Windows-vindue via WebView2. Den samme kode kan senere lægges på en hjemmeside (se [Fra Windows-program til hjemmeside](#fra-windows-program-til-hjemmeside)).

## Funktioner

| Område | Hvad kan man |
|---|---|
| **Profiler** | Flere børn kan bruge samme computer. Hver får sit eget dyr, sin egen farve og sit klassetrin (5. eller 6.), og klassetrinnet styrer sværhedsgraden. |
| **Regneregler** | Forklaringer med opstillinger og eksempler. Emnerne er plus, minus, gange, division, regnearternes rækkefølge, negative tal, decimaltal, enheder, brøker, procent, ligninger, geometri, koordinatsystem og tekststykker. |
| **Trin for trin** | Skriv et regnestykke, så regner ræven et *lignende* stykke med andre tal, ét trin ad gangen, fx »gange før plus« eller »find fællesnævneren«. Barnet ser metoden uden at få sit eget svar serveret. |
| **Tabeller** | Gangetabellen (op til 12) med markering. Man kan øve en tabel og få grøn eller rød feedback. Der er også tabeller over brøk, decimaltal og procent, over enheder og over kvadrattal. |
| **Indtal opgaver** | Tryk 🎤 Indtal og sig opgaven. I Windows-programmet starter Windows' stemmeskrivning (samme som Win + H), som forstår dansk. Ræven oversætter talte regnestykker til tal og tegn: »tre fjerdedele plus en sjettedel« → `3/4 + 1/6`, »start parentes tolv minus fire slut parentes gange tre« → `(12 - 4) · 3`. På en hjemmeside bruges browserens talegenkendelse. |
| **Mine opgaver** | Gem opgaver fra bogen eller lektierne. Ræven foreslår tags ud fra teksten (fx Brøker og Plus), og man kan selv tilføje tags som `lektier uge 48`. Opgaverne kan søges og filtreres på tag og status. |
| **Lommeregner** | Lommeregneren er **låst**, indtil barnet har skrevet sit eget svar. Derefter kan svaret tjekkes med lommeregneren, med et automatisk tjek (for regnestykker) og med udregningen trin for trin. |
| **Regnespil** | Der er 8 daglige opgaver på tværs af pensum og en træning, hvor man selv vælger emne og sværhedsgrad. |

### XP og titler

- Simple opgaver giver lidt XP, og svære emner giver mere: plus og minus 5, gange og division 8, rækkefølge og negative tal 12, decimaltal og enheder 15, brøker, procent og ligninger 20, geometri og koordinatsystem 25.
- Sværhedsgraden ganger XP'en: let ×1, middel ×1,5, svær ×2.
- Rigtigt svar i første forsøg giver fuld XP, og i andet forsøg halv XP. Derefter vises løsningen.
- Dagens opgaver giver fuld XP, og træning giver halv XP. Når alle dagens opgaver er klaret, får man en bonus, der vokser med antallet af dage i træk.
- Fra level til level skal der bruges 100, 125, 150 … XP. En ny rævetitel låses op hvert 5.–10. level: Rævehvalp → Nysgerrig ræv → Snu ræv → Lynræv → Rødræv → Sølvræv → Polarræv → Stjerneræv → Guldræv → Mesterræv → Legendarisk regneræv.

## Kom i gang

Forudsætninger: Windows 10/11 med WebView2, som er indbygget i Windows 11, og .NET 8 SDK for at bygge.

**Byg et færdigt program** til `publish\Regneraeven\Regneraeven.exe`. Det er selvstændigt, så .NET skal ikke installeres på den pc, det kører på:

```powershell
.\publish.ps1
```

**Kør under udvikling** som vindue:

```powershell
dotnet run --project src/Regneraeven.Desktop --launch-profile desktop
```

**Kør i en browser** under udvikling på http://localhost:5187. Adgangslinket med nøgle står i konsollen:

```powershell
dotnet run --project src/Regneraeven.Desktop --launch-profile browser
```

**Tests:**

```powershell
dotnet test
```

Data ligger i en SQLite-database i `%APPDATA%\Regneraeven\regneraeven.db`. Udviklingsprofilerne bruger i stedet `src/Regneraeven.Desktop/App_Data/dev` (via `REGNERAEVEN_DATA`), og den mappe er ikke med i git.

## Struktur

```
src/Regneraeven.Core/      Ren .NET-kode uden web eller Windows:
  Maths/                   Brøkregning (Rational), parser + trin-for-trin-løser, læsning af børns svar
  Tasks/                   Opgavegenerator (13 emner × 3 sværhedsgrader), svar-tjek, dagens opgaver
  Progress/                XP, levels og rævetitler
  Topics/                  Emner, XP-værdier og forslag til tags
  Help/                    Regnereglerne (tekst)
src/Regneraeven.Web/       Hele appen som Razor class library: controllere, views, css/js, EF Core (SQLite)
src/Regneraeven.Desktop/   Windows-programmet: WinForms + WebView2, server på 127.0.0.1, adgangsnøgle
tests/Regneraeven.Core.Tests/
```

Databaseændringer laves som EF Core-migrations:

```powershell
dotnet tool restore
dotnet ef migrations add <Navn> -p src/Regneraeven.Web -s src/Regneraeven.Desktop -o Data/Migrations
```

## Fra Windows-program til hjemmeside

Al logik og hele brugerfladen ligger i `Regneraeven.Core` og `Regneraeven.Web`. Windows-delen er kun et tyndt lag. En hjemmeside kræver:

1. Et nyt projekt `Regneraeven.Server` (`net8.0`, Microsoft.NET.Sdk.Web), der kalder `builder.Services.AddRegneraeven(...)` og `app.UseRegneraeven()`, ligesom Desktop gør.
2. Login for forældre, fx ASP.NET Core Identity. Profilerne kobles til forælderens konto (stedet er markeret i `Profile`), og `CurrentProfile` tjekker, at profilen tilhører den, der er logget ind.
3. Eventuelt SQL Server eller PostgreSQL i stedet for SQLite. Det er en ny EF-provider plus nye migrations.

## Sikkerhed

- Den indbyggede server lytter kun på `127.0.0.1` på en tilfældig port.
- Hver opstart får en tilfældig adgangsnøgle. Kun programmets eget vindue kender den, så andre programmer og hjemmesider kan ikke tale med serveren.
- Alle formularer er beskyttet mod CSRF (anti-forgery tokens).
- Facit til en spilopgave sendes først til browseren, når opgaven er løst eller opgivet.
