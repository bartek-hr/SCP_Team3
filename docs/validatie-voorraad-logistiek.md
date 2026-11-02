# Validatie voor voorraad en logistiek

Deze afspraken horen bij #84. Ze gelden voor de V1-endpoints van voorraad,
magazijnen, locaties, orders, zendingen, transfers en hun artikelen. #84 bouwt
voort op #73 (klanten en artikelcatalogus). Die issue gebruikt dezelfde
foutaanpak, zodat de hele API op dezelfde manier fouten meldt.

## Foutaanpak

| Status | Betekenis | Voorbeelden |
| --- | --- | --- |
| `400 Bad Request` | De invoer is ongeldig | verplicht veld ontbreekt, negatief aantal, verwijzing naar een record dat niet bestaat, ongeldige JSON |
| `404 Not Found` | Het record uit de URL bestaat niet | `PUT /orders/999` |
| `409 Conflict` | De invoer botst met opgeslagen gegevens | id bestaat al, artikel staat al in de order, record wordt nog gebruikt, transfer is al verwerkt |

Bij `400` en `409` stuurt de API een body volgens RFC 9457 (*problem details*)
met `Content-Type: application/problem+json`:

```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "items[1].item_id 404 does not exist."
}
```

- `detail` noemt het veld met de JSON-naam. In een lijst staat de positie erbij,
  bijvoorbeeld `items[1].amount`.
- De meldingen zijn Engels, net als de rest van het API-contract.
- Een ongeldig id in de URL (`/orders/abc`) geeft nog steeds `400` zonder body.

### Hoe het in de code werkt

1. De logic-laag gooit een `ArgumentException` bij ongeldige invoer en een
   `InvalidOperationException` bij een conflict. De gedeelde controles staan in
   `Logics/Validation.cs`.
2. De handler roept de logic aan via `WriteResponse.Save(...)`. Die zet de
   exception om in `Response.BadRequest(detail)` of `Response.Conflict(detail)`.
3. Als vangnet geeft een `DbUpdateException` (bijvoorbeeld twee gelijktijdige
   requests met hetzelfde artikel) een `409` in plaats van een `500`.
4. OpenAPI documenteert de `400` van elk endpoint met een body als
   `application/problem+json`.

## Regels per onderdeel

Een **verwijzing** (`*_id`) moet naar een bestaand record wijzen; `0` of een
ontbrekend veld geldt als niet-bestaand. Een `id` in de body mag bij `PUT`
wegblijven, maar moet anders gelijk zijn aan het id in de URL.

| Onderdeel | 400 (ongeldige invoer) | 409 (conflict) |
| --- | --- | --- |
| Voorraad `/inventories` | `item_id` en `location_id` bestaan; alle `quantity_*` ≥ 0; bij `PUT` horen `item_id` en `location_id` bij de URL | – |
| Magazijnen `/warehouses` | alle velden verplicht; geldig `contact_email` | id bestaat al; verwijderen terwijl er locaties of orders naar verwijzen |
| Locaties `/locations` | `code` en `name` verplicht; `warehouse_id` bestaat | id bestaat al; verwijderen terwijl transfers of voorraad naar de locatie verwijzen |
| Orders `/orders` | `client_id`, `ship_to_client_id`, `bill_to_client_id` en `warehouse_id` bestaan; `order_date` en `request_date` verplicht en `request_date` ≥ `order_date`; `reference`, `customer_po_number` en `order_status` verplicht; artikelen (zie onder) met `unit_price` ≥ 0 | id bestaat al; verwijderen terwijl zendingen naar de order verwijzen |
| Zendingen `/shipments` | `order_id` (optioneel) bestaat, ook voor elk id bij `PUT /shipments/{id}/orders`; `shipment_date` en alle tekstvelden verplicht; artikelen (zie onder) | id bestaat al |
| Transfers `/transfers` | `reference` verplicht; `from_location_id` en `to_location_id` bestaan en verschillen; artikelen (zie onder), bij `PUT` alleen als ze worden meegestuurd | id bestaat al; een transfer met status `Processed` wijzigen |
| Artikellijsten (`items` van orders, zendingen en transfers) | `item_id` bestaat; `amount` > 0; elk `item_id` komt één keer voor | – |
| `/orderitems` | `order_id` en `item_id` bestaan; `amount` > 0; `unit_price` ≥ 0 | artikel staat al in die order |
| `/ShipmentItems` | `shipment_id` en `item_id` bestaan; `amount` > 0 | artikel staat al in die zending |
| `/TransferItems` | `transfer_id` en `item_id` bestaan; `amount` > 0 | artikel staat al in die transfer; transfer is al verwerkt (geldt voor toevoegen, wijzigen en verwijderen) |

Een verwerkte transfer heeft de voorraad al verplaatst. Als je hem daarna
wijzigt, kloppen de transfer en de voorraad niet meer met elkaar. Daarom geeft
dat een `409`. Verwijderen van een hele transfer blijft toegestaan, net als in
de Python-API.

## Gedragswijzigingen

| Situatie | Voorheen | Nu |
| --- | --- | --- |
| Foutmelding bij 400/409 | lege body | problem details met `detail` |
| Ongeldige JSON | `400` zonder body | `400` met het veld, bijvoorbeeld `at $.quantity_on_hand` |
| Verwijzing naar onbekend artikel, klant, magazijn, locatie of order | opgeslagen | `400` |
| `request_date` vóór `order_date` | opgeslagen | `400` |
| `POST /orderitems` met `amount` 0 of negatief | opgeslagen | `400` |
| Hetzelfde artikel dubbel via `/orderitems` of `/ShipmentItems` | `500` | `409` |
| Hetzelfde artikel dubbel in de `items` van een transfer | opgeslagen | `400` (bij orders en zendingen al zo) |
| `id` in body wijkt af van URL bij magazijn, locatie, transfer of item-endpoints | genegeerd | `400` (orders en zendingen deden dit al) |
| Locatie met voorraad verwijderen | verwijderd, voorraad bleef achter | `409` |
| Magazijn verwijderen waar orders naar verwijzen | verwijderd | `409` |
| Order verwijderen waar zendingen naar verwijzen | verwijderd | `409` |
| Verwerkte transfer wijzigen | toegestaan | `409` |
| `GET /TransferItems/{id}/items` | altijd `400` (las de verkeerde parameter) | geeft de artikelen van de transfer |

## Bewust niet gecontroleerd

- **Statuswaarden** (`order_status`, `shipment_status`, `shipment_type`,
  `shipping_method`, `payment_type`). In de legacy-data komen alleen vaste
  waarden voor, zoals `Pending`/`Shipped` en `Scheduled`/`Transit`/`Delivered`,
  maar de Python-API accepteerde elke tekst. Of dit een vaste lijst wordt,
  beslist de PO.
- **`quantity_allocated` ≤ `quantity_on_hand`.** Het orderproces
  (`PUT /orders/{id}/items`) reserveert zelf al zonder die grens. Alleen
  `PUT /inventories` begrenzen zou de regels tegenstrijdig maken.
- **Voorraad bij losse item-endpoints.** `/orderitems` en `/ShipmentItems`
  werken de voorraad niet bij; `PUT /orders/{id}/items` en
  `PUT /shipments/{id}/items` wel. Dat verschil bestond al en valt buiten
  validatie.

## Bekende beperking: `POST /TransferItems`

`TransferItem.TransferId` heeft `[JsonIgnore]`, zodat `GET /transfers` alleen
`item_id` en `amount` toont, net als de Python-API. Daardoor leest
`POST /TransferItems` een meegestuurde `transfer_id` niet in. De API geeft nu
een duidelijke `400` (`transfer_id 0 does not exist.`) in plaats van een `500`.
Het endpoint echt bruikbaar maken kan met een eigen request-model of met een
route als `POST /transfers/{id}/items`. Die keuze ligt bij de maker van de
TransferItem-endpoints.

## Controle tegen bestaande gegevens

Alle regels hierboven zijn gecontroleerd op de legacy-data in `legacy/data`.
Er zijn geen overtredingen: geen onbekende verwijzingen, geen negatieve
aantallen of `amount` ≤ 0, geen `request_date` vóór `order_date`, geen
transfer naar dezelfde locatie en geen dubbele artikelen. Het herstelrapport
van #74 gebruikt dezelfde verwijzingsregels.

**Let op:** de migraties importeren de artikelcatalogus (`Items`, `ItemLines`,
`ItemGroups`) nog niet. In een database die met de huidige migraties is
opgebouwd zijn die tabellen leeg, terwijl voorraad, orders, zendingen en
transfers wel naar 600 legacy-artikelen verwijzen. Tot de artikelen ook worden
geïmporteerd, weigert de API elke wijziging met een `item_id` met
`item_id … does not exist.`. Klanten, magazijnen, locaties, orders en
zendingen zijn wel geïmporteerd, dus die verwijzingen werken.

## Tests

- `CargoHUB.Tests/Logics/*LogicTests.cs`: geldige en ongeldige invoer per
  onderdeel, inclusief de precieze foutmelding (`AssertInvalid` en
  `AssertConflict` in `DatabaseTest`).
- `CargoHUB.Tests/Handlers/*HandlerTests.cs`: statuscode en problem-details-body
  via de echte routes (`AssertProblemAsync` in `HandlerTest`).
- `CargoHUB.Tests/Framework/ResponseTests.cs`: het problem-details-formaat.

```bash
dotnet test CargoHUB.Tests
```
