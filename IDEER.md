# Ideer til Regneræven 🦊

Her samler vi ideer. Skriv dem ind under **Nye ideer**, og flyt dem videre, når de bliver besluttet eller lavet.

## Nye ideer

- **Guld og mit rævehule-værelse:** ud over XP optjener man guld 🪙 fra opgaverne og bruger det på ting til sit eget ræve-værelse.
  - XP kan aldrig bruges (det er ens level); guld kan bruges i butikken. Begge stiger, når man løser opgaver.
  - Guld pr. opgave følger sværhedsgraden (som XP), plus guld-bonus for dagens opgaver og dage i træk. Ingen guld for opgivne opgaver.
  - **Butik** med kategorier: seng, puder og sengetøj, vægfarve og tapet, skrivebord og stol, lamper og lys på væggen (lyskæde, neonskilt), tæpper, plakater og billeder, planter, reoler og legetøj.
  - **Mit værelse:** et tegnet værelse set forfra, hvor hver ting har sin plads (seng, skrivebord, væg, gulv, vindue). Man vælger, hvilken af sine ting der står hvor, og kan skifte når som helst.
  - Nogle ting låses først op ved bestemte levels/titler (fx et guld-skrivebord ved Guldræv), så de også passer sammen med unlocks under profilen.
  - Senere: frit at flytte ting rundt, vise sit værelse til søskende-profilerne, sæsonting (jul, halloween).
  - Kun guld fra opgaverne – aldrig rigtige penge.
  - Kræver en del tegninger (SVG) – start med et lille sæt pr. kategori og udvid løbende.
- **Mere 10-finger:** en daglig skriveopgave ved siden af dagens regneopgaver, egne titler/unlocks for skrivning (fx »Lynpote«), to tegnede hænder der viser fingeren, og guld pr. lektion når guld findes.

## Planlagt

- **Hjemmeside i drift:** følg `docs/azure.md` for at oprette den i Azure (ca. 0 kr./md.).
- **Domæne:** tjek om `regneraeven.dk` er ledigt (punktum.dk).
- **Hjemmeside-forbedringer:** »Glemt adgangskode« via e-mail, managed identity til databasen, automatisk udrulning fra GitHub Actions.

## Mulige forbedringer

- Flere opgavetyper: cirklens omkreds og areal, gennemsnit, rumfang, diagrammer og aflæsning.
- Tekststykker i spillet, ikke kun regnestykker.
- Badges ud over titlerne, fx »10 brøker i træk« eller »7 dage i træk«.
- Statistik for forældre: hvilke emner barnet har svært ved.
- Tabel-quiz på tid i Tabeller.
- Mørkt tema.
- Lyd og animation ved rigtige svar, som man kan slå fra.
- Billede af opgaven fra bogen gemt sammen med en gemt opgave.

## Lavet

- **Hjemmeside-udgave forberedt til Azure** (23. nov. 2026): server-projekt med forælder-login og Azure SQL, Dockerfile, GitHub Actions og Bicep-skabelon.
- **GitHub** (23. nov. 2026): privat repo Mithione/regne-raeven.

- **Læs regnereglerne højt** (23. nov. 2026): 🔊-knapper på reglerne, matematik læses som ord, kvinde-/mandestemme og tempo under profilen (kun dansk kvindestemme på computeren i dag).

- **Bonus for dage i træk** (23. nov. 2026): milepæle ved 3, 7, 14, 30, 50 og 100 dage med XP-bonus og selvvalgt eksklusiv belønning, fridage (én pr. 7 dage, højst 2), rigtige belønninger fra forældre, flamme-kort på forsiden.

- **Lektier med dato** (23. nov. 2026): flueben + afleveringsdato, »Lektier der skal laves« på forsiden med forsinkede markeret, filter »Lektier« sorteret efter dato.

- **Lær 10-finger-systemet** (23. nov. 2026): 18 lektioner på et tegnet dansk tastatur med fingerfarver, præcision og ord/min, 1–3 stjerner og XP pr. ny stjerne, lektion med regnestykker.

- **Flere slags unlocks** (23. nov. 2026): rammer om avataren, baggrunde og farvetemaer under »Mine unlocks«, låst op mellem titel-levels. Kun ræve som avatar – startdyrene er fjernet.

- **Rævetitler og unlocks under profilen** (23. nov. 2026): titler flyttet til »Din profil«, 11 tegnede ræve låses op ved titel-levels, gråtonede låste ræve, »Brug den nu« i level-up-dialogen.

- **Indtal opgaver med mikrofonen** (23. nov. 2026): 🎤-knap under »Gem en opgave«, der starter Windows' stemmeskrivning (dansk). Talte regnestykker oversættes til tal og tegn med et »Brug det«-forslag. Browserens talegenkendelse bruges, hvis det bliver en hjemmeside.

- Første version: regneregler, trin for trin, tabeller, gemte opgaver med tags, låst lommeregner, regnespil med XP og rævetitler (23. nov. 2026).
