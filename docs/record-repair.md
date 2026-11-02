# Corrupte records herkennen en herstellen (#74)

Deze beheeropdracht werkt **offline op een bestaande C# SQLite-database**. Er is
geen nieuw publiek endpoint en er worden geen migrations, imports of reparaties
bij het normale starten uitgevoerd. De legacy JSON-bronnen blijven ongewijzigd.

## Uitvoeren

Stop schrijvers naar de gekozen database. Maak eerst een consistente backup met
de beheerprocedure uit #76; deze opdracht implementeert geen backupbeheer.
Probeer herstel eerst op een afzonderlijke kopie. Gebruik dezelfde applicatieversie
als het databaseschema. Een ontbrekende database of tabel veroorzaakt een fout;
er wordt geen lege database aangemaakt of schema automatisch gemigreerd.

```bash
# Bouw eenmaal, daarna uitsluitend het JSON-rapport op stdout.
dotnet build CargoHUB.csproj
dotnet bin/Debug/net10.0/CargoHUB.dll --repair-records /pad/naar/kopie.sqlite > preview.json

# Pas alleen de beschreven herstelbare gevallen toe.
dotnet bin/Debug/net10.0/CargoHUB.dll --repair-records /pad/naar/kopie.sqlite --apply > applied.json
```

Zonder `--apply` opent de opdracht de database alleen-lezen. Exitcodes: `0` voltooid
zonder openstaande beoordelingen; `3` voltooid met `NeedsReview` (ook bij succesvolle
reparaties van andere records); `2` ongeldige argumenten; `1` uitvoering mislukt.
Controleer het rapport, niet alleen de exitcode. Een uitvoerfout maakt geen
succesrapport. Bij databasefouten worden wijzigingen en audit samen teruggedraaid.
Beveilig rapporten en backups als de database zelf: ze bevatten originele gegevens,
inclusief contactgegevens. Het rapport kan groot zijn.

## Voorlopige herkennings- en herstelregels: `74-v1`

Issue #74 bevat geen goedgekeurde PO-regels of aanvullende reacties. Deze expliciete,
conservatieve regels zijn daarom voorlopig en betekenen **geen PO-goedkeuring**.
Alle tabellen uit het huidige EF-model worden op ruwe opgeslagen waarden onderzocht,
zodat een onleesbare datum geen volledige materialisatie van records blokkeert.

| Herkenning | Uitkomst |
| --- | --- |
| Omringende whitespace in `Name`, `Description`, `Address`, `City`, `Province`, `Country`, `ContactName` met verder bruikbare tekst | `Recoverable` in preview; `Repaired` na toepassen van `Trim()` |
| Ontbrekende niet-nullbare waarde, leeg verplicht tekstveld volgens model of bestaande logistieke validatie, ongeldig contact-emailadres | `NeedsReview`; niets invullen of raden |
| Ongeldig getal, niet-positieve sleutel/verwijzing/itemhoeveelheid, negatieve voorraad/gewicht/verpakkingshoeveelheid/prijs | `NeedsReview`; geen bedragen of voorraad aanpassen |
| Ontbrekende gerelateerde klant, leverancier, artikel, classificatie, magazijn, locatie, order, zending of transfer | `NeedsReview`; geen koppeling wijzigen of gerelateerd record aanmaken |
| Ontbrekende/onleesbare/default datum, `UpdatedAt` vóór `CreatedAt`, onbekende enumwaarde | `NeedsReview`; geen datum of status verzinnen |

Vrije optionele tekst mag leeg blijven. Nullbare verwijzingen (zoals een zending
zonder order) mogen null blijven. Barcode, codes, referenties, ordernummers,
telefoonnummers, postcodes, statussen en alle sleutels worden nooit genormaliseerd.
De herstelde beschrijvende tekst behoudt hoofdletters en interne whitespace.
Een record met ook maar één niet-herstelbaar probleem wordt **helemaal niet**
gewijzigd, ook niet wanneer een ander veld herstelbaar is. Gezonde records staan
niet in `Records`; `Scanned` telt ze wel mee. `NeedsReview` betekent dat de opdracht
geen veilige automatische oplossing kent, niet dat gegevens definitief verloren zijn.

Deze herkenning is geen volledige bedrijfsvalidatie. Dubbele ordernummers,
voorraadberekeningen, toegestane logistieke overgangen en domeinspecifieke
betekenissen van tekst worden niet geraden. Fysiek beschadigde SQLite-bestanden
of ontbrekende tabellen stoppen de opdracht; dit is geen SQLite-bestandsherstel.

## Gegevensbehoud en overdracht naar #85

Een apply-run neemt een schrijftransactie voordat de momentopname wordt gelezen.
Alle updates en de audit committen samen. Alleen beschrijvende velden veranderen;
IDs, koppelingen, aantallen, datums en gerelateerde rijen blijven behouden. De
historische `UpdatedAt` blijft intact; het audittijdstip registreert het herstel.
Een herhaalde run herstelt reeds genormaliseerde tekst niet opnieuw.

`RecordRepairRuns` wordt alleen bij apply aangemaakt. Elke run bevat `RunId`,
`RulesVersion`, UTC `RecordedAt`, `Scanned` en `ReportJson`. Dat rapport bevat per
gevonden record de tabel, volledige (ook samengestelde) `Key`, `Outcome`,
`Reasons` met veld/regel/herstelbaarheid en volledige `Before`/`After`-momentopnamen.
Ook `NeedsReview` wordt vastgelegd. De audit heeft bewust geen cascade-FKs naar
bedrijfsgegevens, zodat later archiveren het bewijs niet verwijdert. Iedere apply
maakt een nieuwe auditrun; herhaalde beoordelingen blijven traceerbaar.

#85 kan `NeedsReview` en de originele snapshots gebruiken. Deze branch archiveert
of verwijdert geen records en bepaalt geen archiveringsbeleid. De actuele database
blijft de bron; een oud rapport moet bij latere archivering opnieuw op actualiteit
worden gecontroleerd. Er is geen automatisch terugzetten vanuit audit: daarvoor
zijn expliciete keuzes en controle van tussentijdse wijzigingen nodig.

## PO-besluiten en grenzen aan teamwerk

Nog nodig: bevestig de whitespace-regel en herkenningsregels, bepaal hoe ontbrekende
of strijdige waarden met een gezaghebbende bron mogen worden hersteld, en bepaal
archiveringskeuze/bewaartermijn voor #85. Tot die tijd blijven twijfelgevallen staan.

- #74 is huidige sprint, toegewezen aan JEMostert; alleen deze offline herstelstroom is gewijzigd.
- #73/#84 (Floris831) blijven eigenaar van API-invoer- en bedrijfsvalidatie. Er zijn
  geen handlers, bestaande logica of gedeelde validators aangepast. Stem nieuwe
  regels later af en versieer wijzigingen in de herstelregels expliciet.
- #75 (DeSadGuy) en vervolg #86 blijven eigenaar van oorspronkelijke ordernummers
  en hernummering. Deze opdracht wijzigt geen orderidentiteit of referentie en
  implementeert geen gedeeld nummeringsschema.
- #76 (bartek-hr) blijft eigenaar van dagelijkse backups en herstelprocedures.
- #85 is het afzonderlijke vervolg voor archivering; integratie is uitgesteld.
- #82 loopt afzonderlijk; de vroege maintenance-dispatch in `Program.cs` mag bij
  integratie niet via hosting, migrations of V1/V2-routing worden uitgevoerd.

## Verificatie

De gerichte SQLite-journeytests in `RecordRepairJourneyTests` controleren preview,
toepassen, opnieuw uitvoeren, relatiebehoud, volledige audit, behoud van corrupte
datums en samengestelde sleutels, negatieve voorraad, rollback bij auditfouten en
de CLI met een afzonderlijke tijdelijke database. Er is geen browsersurface voor
deze opdracht; bestaande HTTP-routes zijn niet aangepast.

```bash
dotnet test CargoHUB.Tests --filter FullyQualifiedName~RecordRepairJourneyTests
```

Bij controle van een afzonderlijke database uit de huidige migrations werden
79.848 rijen gescand. Daarvan bleven 67.328 rijen herkenbaar als `NeedsReview`
wegens ontbrekende artikelverwijzingen. Er waren in deze seed geen tekstgevallen
die de opdracht automatisch kon herstellen. Apply bewaarde deze rijen en legde
ze vast in één auditrun; gerichte fixtures oefenen de daadwerkelijke tekstreparatie.
Dit is geen toestemming om ontbrekende artikelen te fabriceren of rijen te verwijderen.
