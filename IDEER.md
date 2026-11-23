# Ideer til Regneræven 🦊

Her samler vi ideer. Skriv dem ind under **Nye ideer**, og flyt dem videre, når de bliver besluttet eller lavet.

## Nye ideer

- **Indtal opgaver med mikrofonen** under »Mine opgaver«, så de er nemmere at oprette.
  - Virker allerede i dag uden ny kode: Windows' stemmeskrivning (**Win + H**) i tekstfeltet. Den skal sættes til dansk.
  - En mikrofon-knap i programmet: Windows' egen talegenkendelse via Windows-delen (kræver dansk talepakke). På en senere hjemmeside: browserens talegenkendelse.
  - Omsæt talte tal til regnestykker: »tre fjerdedele plus en sjettedel« → `3/4 + 1/6`, »to komma fem gange fire« → `2,5 · 4`.
- **Lektier:** et flueben »Det er en lektie« og en afleveringsdato på en gemt opgave.
  - Filter »Lektier« i Mine opgaver, sorteret efter dato.
  - »Lektier der skal afleveres snart« på forsiden, markeret når de er forsinkede.
- **Læs regnereglerne højt** med en rolig stemme: en »🔊 Læs op«-knap på hver regel (og evt. på hvert afsnit).
  - I profilen: vælg **kvinde- eller mandestemme** og evt. tempo (langsom/normal).
  - Teknik: browserens/Windows' indbyggede oplæsning (`speechSynthesis`), sat lidt langsommere for en rolig stemme. Opstillinger og tegn som »·«, »:« og »3/4« skal skrives om til ord (»gange«, »divideret med«, »tre fjerdedele«), før de læses op.
  - Tjek først hvilke danske stemmer der findes på pc'en. Windows har typisk én dansk kvindestemme som standard; en dansk mandestemme kan kræve ekstra stemmer eller en online-tjeneste. Findes kun én, kan valget skjules.
- **Rævetitler og unlocks under profilen:** flyt titel-oversigten fra Regnespil ind på »Din profil«, så profilen bliver stedet for alt det, man har låst op.
  - Nyt område »Mine unlocks«: nye, smartere avatars, der låses op ved bestemte levels/titler (fx en ræv med briller ved Snu ræv, astronaut-ræv ved Stjerneræv, ræv med krone ved Mesterræv).
  - Låste avatars vises gråtonede med »Låses op ved level X«, så der er noget at stræbe efter.
  - Ved level-up: dialogen fortæller også, hvis der er en ny avatar, med en knap »Brug den nu«.
  - Senere: andre slags unlocks, fx farvetemaer eller rammer om avataren.
  - Kræver tegnede avatars (SVG i samme stil som ræve-logoet) i stedet for emojis.
- **Guld og mit rævehule-værelse:** ud over XP optjener man guld 🪙 fra opgaverne og bruger det på ting til sit eget ræve-værelse.
  - XP kan aldrig bruges (det er ens level); guld kan bruges i butikken. Begge stiger, når man løser opgaver.
  - Guld pr. opgave følger sværhedsgraden (som XP), plus guld-bonus for dagens opgaver og dage i træk. Ingen guld for opgivne opgaver.
  - **Butik** med kategorier: seng, puder og sengetøj, vægfarve og tapet, skrivebord og stol, lamper og lys på væggen (lyskæde, neonskilt), tæpper, plakater og billeder, planter, reoler og legetøj.
  - **Mit værelse:** et tegnet værelse set forfra, hvor hver ting har sin plads (seng, skrivebord, væg, gulv, vindue). Man vælger, hvilken af sine ting der står hvor, og kan skifte når som helst.
  - Nogle ting låses først op ved bestemte levels/titler (fx et guld-skrivebord ved Guldræv), så de også passer sammen med unlocks under profilen.
  - Senere: frit at flytte ting rundt, vise sit værelse til søskende-profilerne, sæsonting (jul, halloween).
  - Kun guld fra opgaverne – aldrig rigtige penge.
  - Kræver en del tegninger (SVG) – start med et lille sæt pr. kategori og udvid løbende.

## Planlagt

- **GitHub:** opret repo under Mithione-kontoen og push.
- **Hjemmeside:** nyt host-projekt (`Regneraeven.Server`), login for forældre og profiler koblet til forælderens konto. Se README → »Fra Windows-program til hjemmeside«.
- **Domæne:** tjek om `regneraeven.dk` er ledigt (punktum.dk), før det bliver en hjemmeside.

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

- Første version: regneregler, trin for trin, tabeller, gemte opgaver med tags, låst lommeregner, regnespil med XP og rævetitler (23. nov. 2026).
