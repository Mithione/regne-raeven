# Ideer til Regneræven 🦊

Her samler vi ideer. Skriv dem ind under **Nye ideer**, og flyt dem videre, når de bliver besluttet eller lavet.

## Nye ideer

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
- **Lær 10-finger-systemet:** et nyt område, hvor man træner at skrive på tastaturet – og får XP og guld for det.
  - Lektioner i fast rækkefølge: grundrækken (asdf jklæ) → øverste række → nederste række → æ, ø, å → store bogstaver → tal og tegn.
  - Tegnet **dansk tastatur** på skærmen, hvor hver tast har farven på den finger, der skal trykke, og næste tast lyser op. Evt. to hænder, der viser fingeren.
  - Måling af **præcision** og **hastighed** (ord pr. minut). Præcision tæller mest – man skal hellere skrive rigtigt end hurtigt.
  - Kobling til matematik: en lektion med taltasterne og regnetegn, hvor man skriver regnestykker (`3/4 + 1/6`).
  - XP og guld pr. lektion efter præcision og sværhedsgrad; stjerner (1–3) pr. lektion, som man kan forbedre.
  - Evt. en daglig skriveopgave ved siden af dagens regneopgaver, og egne rævetitler/unlocks for skrivning (fx »Lynpote«).
- **Bonus for dage i træk:** milepæle ved fx **10, 20 og 50 dage i træk**, hvor man låser noget særligt op.
  - Man vælger selv, hvad hver milepæl skal give, fx på en »Mine mål«-side: en sjælden avatar, en speciel ting til værelset, en stor guld-bunke eller en særlig titel.
  - Mulighed for at en forælder kan skrive en **rigtig belønning** ind (fx »Is på fredag« eller »Vælg aftensmad«), som vises, når milepælen nås.
  - En flamme-tæller 🔥 på forsiden, der viser, hvor langt der er til næste milepæl (»3 dage til 10-dages-bonus«).
  - Overvej en »fridag« (fx én pr. uge eller købt for guld), så man ikke mister hele sin stime ved én glemt dag.

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

- **Indtal opgaver med mikrofonen** (23. nov. 2026): 🎤-knap under »Gem en opgave«, der starter Windows' stemmeskrivning (dansk). Talte regnestykker oversættes til tal og tegn med et »Brug det«-forslag. Browserens talegenkendelse bruges, hvis det bliver en hjemmeside.

- Første version: regneregler, trin for trin, tabeller, gemte opgaver med tags, låst lommeregner, regnespil med XP og rævetitler (23. nov. 2026).
