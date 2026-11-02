# C# V1 en V2 naast elkaar

Deze afspraken horen bij #82. V1 is het bestaande **C#-contract** onder
`/api/v1`; de Python-API en haar documentatie blijven migratiereferenties.
Dit document claimt geen volledige gelijkwaardigheid met Python.

## Versiegrenzen

| Onderdeel | V1 | V2 |
| --- | --- | --- |
| HTTP-prefix | `/api/v1` | `/api/v2` |
| Route-attribuut | `[Get("/clients")]` (standaard versie 1) | `[Get("/clients", version: 2)]` |
| OpenAPI | `/openapi/v1.json`, versie 1.0 | `/openapi/v2.json`, versie 2.0 |
| Status | Bestaande C#-functionaliteit | Ruimte voor afzonderlijk opgeleverde uitbreidingen |

Route discovery ondersteunt `CargoHUB.Handlers` en subnamespaces. Laat bestaande
handlers staan om onnodige verplaatsingen en conflicten met teamwerk te voorkomen.
Plaats nieuwe V2-handlers in `Handlers/V2/` met namespace `CargoHUB.Handlers.V2`
en **expliciete** `version: 2` op ieder route-attribuut. De namespace bepaalt de
HTTP-versie niet. Zonder expliciete versie blijft een route V1.

V2 wordt niet automatisch een kopie van V1. Een niet-opgeleverde V2-route geeft
404; er is geen fallback, redirect of automatische upgrade van V1-requests.
V2 bevat bij deze oplevering nog geen productie-endpoints; het V2-document is
bewust leeg. Testfixtures bewijzen dat verschillende contracten voor hetzelfde
resource tegelijk kunnen werken. Die fixtures staan alleen in het testproject.

OpenAPI-documenten bevatten uitsluitend routes uit hun eigen versie. Operation
IDs moeten binnen één versie uniek zijn; dezelfde naam mag in V1 en V2 worden
gebruikt, omdat clients uit afzonderlijke documenten worden gegenereerd.
Swagger biedt beide documenten aan en kiest standaard V1.

## Compatibiliteitsafspraken

- Behoud bestaande V1-paden, HTTP-methoden, veldnamen, JSON-vormen, statuscodes,
  validatie en foutafhandeling wanneer V2 wordt toegevoegd. Maak geen globale
  serializer- of middlewarewijziging uitsluitend voor een V2-behoefte.
- Een onverenigbare wijziging krijgt een expliciete V2-handler en een eigen
  request/response DTO zodra het contract afwijkt. Deel V1-modellen niet met een
  afwijkend V2-wirecontract. Een methode mag alleen beide route-attributen dragen
  wanneer het HTTP-contract werkelijk identiek is.
- Domeinlogica, data-access en SQLite mogen gedeeld blijven als bestaand gedrag
  behouden blijft. Versies zijn geen aparte datasets: schrijfoperaties kunnen
  dezelfde gegevens veranderen. Schemawijzigingen en gedeelde logica vereisen
  daarom ook controle van de V1-contracten en bestaande gegevens.
- V1 wordt niet door deze wijziging uitgefaseerd. Een toekomstige beëindiging
  vereist een aparte teamafspraak, migratiepad en communicatie met afnemers.

## Teamgrenzen en vervolg

#82 levert uitsluitend de versiegrens en bescherming tegen V1-regressies.
Paginering (#78, DeSadGuy), gebruikers/rollen, OAuth en rechten (#77/#81/#87,
RMJTromp), en V1-validatie (#73/#84, Floris831) blijven bij hun eigen issues en
makers. Deze branch implementeert of integreert hun functies niet. Nieuwe
V2-handlers kunnen later expliciet versie 2 gebruiken; contracten en toegangseisen
moeten daarbij door de betreffende makers worden ingevuld.

Python-verwijdering en bewijs van migratiepariteit blijven vervolgissue #88.
Deze wijziging verwijdert geen Python-code, referentiedata of migrations en heeft
geen afhankelijkheid van nog niet opgeleverde V2-bedrijfsfunctionaliteit.

## Verificatie

`ApiVersionJourneyTests` start een HTTP-testhost met de echte V1-handlers en een
SQLite-testdatabase, eerst zonder en daarna met test-only V2-handlers. De journey
vergelijkt alle productie-V1-OpenAPI-paden en schema's, controleert documentscheiding
en dezelfde operation ID in verschillende versies, en voert V1-client CRUD uit.
Ook ongeldige JSON, ontbrekende resources en ontbrekende V2/V3-routes worden
gecontroleerd. De bestaande V1-handlertests blijven de detailcontracten bewaken.

```bash
dotnet test CargoHUB.Tests --configuration Release --filter FullyQualifiedName~ApiVersionJourneyTests
```
