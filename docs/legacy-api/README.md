# Legacy CargoHub API — endpoints & request bodies

Referentie voor de legacy Python-API in [legacy/api/main.py](../../legacy/api/main.py), afgeleid uit de broncode en de datasets in `legacy/data/`. Bedoeld als basis voor characterization tests van de oude API voordat we gaan refactoren.

- Alle voorbeeld-request-bodies staan als losse bestanden in [request-bodies/](request-bodies/).
- Alle requests kun je direct afvuren vanuit Visual Studio / VS Code met [legacy-api.http](legacy-api.http).

## Server starten

```bash
cd legacy/api
python main.py
```

Draait op `http://localhost:3000` (Python 3.10+ nodig vanwege `match`-statements). De data wordt gelezen uit en weggeschreven naar `legacy/data/*.json` — **schrijfacties muteren die bestanden dus echt**. Maak vóór het testen een backup of gebruik een git-checkout om terug te zetten.

## Authenticatie

Elke request heeft een `API_KEY` header nodig (letterlijk die headernaam). Zonder geldige key: **401**. Geldige key maar geen recht op resource+methode: **403**. De keys staan in `legacy/data/user.json`:

| API key | App | Rechten (samengevat) |
|---|---|---|
| `d4s2a0b0a1n4a0l0y7t` | analytics_dashboard | alles alleen GET |
| `s8m3a9r2t7g1l4a5s0s` | smartglass_reader | GET + PUT op transfers, inventories |
| `o3r5d4e2r1p6i8c0k0e` | order_picker | GET + PUT op transfers, orders, inventories |
| `r2e4c6e8i0v3i5n7g9s` | receiving_station | GET + POST/PUT op transfers, items, item_lines/groups/types, suppliers, orders, shipments, inventories |
| `s1h3i5p7p9i2n4g6s8t` | shipping_station | GET + POST/PUT op transfers, clients, shipments; PUT op orders, inventories |
| `f4a5c6i7l8i9t0y1m2a3n4a5g6` | facility_management | GET/POST/PUT/**DELETE** op warehouses, locations, item_lines/groups/types, suppliers, clients |

Let op: **geen enkele key** heeft DELETE-rechten op transfers, items, orders, shipments of inventories. Die DELETE-endpoints bestaan in de code maar zijn in de praktijk altijd 403.

## Endpoints

Basis-URL: `/api/v1`. Kolom "Body" verwijst naar een bestand in `request-bodies/`.

### Warehouses
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/warehouses` | – | 200, lijst |
| GET | `/warehouses/{id}` | – | 200, object (of `null`) |
| GET | `/warehouses/{id}/locations` | – | 200, lijst |
| POST | `/warehouses` | `POST_warehouses.json` | 201, leeg |
| PUT | `/warehouses/{id}` | `PUT_warehouses_id.json` | 200, leeg |
| DELETE | `/warehouses/{id}` | – | 200, leeg |

### Locations
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/locations` | – | 200, lijst |
| GET | `/locations/{id}` | – | 200, object (of `null`) |
| POST | `/locations` | `POST_locations.json` | 201, leeg |
| PUT | `/locations/{id}` | `PUT_locations_id.json` | 200, leeg |
| DELETE | `/locations/{id}` | – | 200, leeg |

### Transfers
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/transfers` | – | 200, lijst (elk object incl. `items`) |
| GET | `/transfers/{id}` | – | 200, object incl. `items` |
| GET | `/transfers/{id}/items` | – | 200, lijst `[{item_id, amount}]` |
| POST | `/transfers` | `POST_transfers.json` | 201, leeg |
| PUT | `/transfers/{id}` | `PUT_transfers_id.json` | 200, leeg |
| PUT | `/transfers/{id}/commit` | **geen body** | 200, leeg |
| DELETE | `/transfers/{id}` | – | altijd 403 (geen key heeft rechten) |

Bij POST wordt `transfer_status` server-side op `"Scheduled"` gezet (wat je zelf meestuurt wordt overschreven). `commit` verplaatst de voorraad van `from_location_id` naar `to_location_id` en zet de status op `"Processed"`.

### Items
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/items` | – | 200, lijst |
| GET | `/items/{id}` | – | 200, object (of `null`) |
| GET | `/items/{id}/inventory` | – | 200, lijst inventory-regels |
| GET | `/items/{id}/inventory/totals` | – | 200, `{total_expected, total_ordered, total_allocated, total_available}` |
| POST | `/items` | `POST_items.json` | 201, leeg |
| PUT | `/items/{id}` | `PUT_items_id.json` | 200, leeg |
| DELETE | `/items/{id}` | – | altijd 403 |

### Item lines / groups / types
Zelfde patroon voor `item_lines`, `item_groups` en `item_types` (`{id, name, description}`):

| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/item_lines` (of `/item_groups`, `/item_types`) | – | 200, lijst |
| GET | `/item_lines/{id}` | – | 200, object (of `null`) |
| GET | `/item_lines/{id}/items` | – | 200, **lijst van item-ids** (alleen ids!) |
| POST | `/item_lines` | `POST_item_lines.json` | 201, leeg |
| PUT | `/item_lines/{id}` | `PUT_item_lines_id.json` | 200, leeg |
| DELETE | `/item_lines/{id}` | – | 200, leeg |

Let op: `GET /suppliers/{id}/items` geeft juist **volledige item-objecten** terug, de andere drie alleen ids.

### Inventories
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/inventories` | – | 200, lijst |
| GET | `/inventories/{id}` | – | **404** (composite key, geen id) |
| POST | `/inventories` | `POST_inventories.json` | 201, leeg — **upsert** op `(item_id, location_id)` |
| PUT | `/inventories/{...}` | – | **404** |
| DELETE | `/inventories/{...}` | – | **404** |

### Suppliers
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/suppliers` | – | 200, lijst |
| GET | `/suppliers/{id}` | – | 200, object (of `null`) |
| GET | `/suppliers/{id}/items` | – | 200, lijst item-objecten |
| POST | `/suppliers` | `POST_suppliers.json` | 201, leeg |
| PUT | `/suppliers/{id}` | `PUT_suppliers_id.json` | 200, leeg |
| DELETE | `/suppliers/{id}` | – | 200, leeg |

Let op: suppliers gebruiken `phone_number` en `reference` in plaats van `contact_phone`/`contact_email` (zoals warehouses/clients).

### Orders
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/orders` | – | 200, lijst (elk object incl. `items`) |
| GET | `/orders/{id}` | – | 200, object incl. `items` |
| GET | `/orders/{id}/items` | – | 200, lijst `[{item_id, amount}]` |
| POST | `/orders` | `POST_orders.json` | 201, leeg |
| PUT | `/orders/{id}` | `PUT_orders_id.json` | 200, leeg |
| PUT | `/orders/{id}/items` | `PUT_orders_id_items.json` (**kale array**) | 200, leeg |
| DELETE | `/orders/{id}` | – | altijd 403 |

`unit_price` per item-regel is optioneel. `PUT /orders/{id}/items` vervangt de regels én werkt `quantity_allocated` in de inventory bij (side effect!).

### Clients
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/clients` | – | 200, lijst |
| GET | `/clients/{id}` | – | 200, object (of `null`) |
| GET | `/clients/{id}/orders` | – | 200, orders waar client koper, ontvanger óf betaler is |
| POST | `/clients` | `POST_clients.json` | 201, leeg |
| PUT | `/clients/{id}` | `PUT_clients_id.json` | 200, leeg |
| DELETE | `/clients/{id}` | – | 200, leeg |

### Shipments
| Methode | Pad | Body | Response |
|---|---|---|---|
| GET | `/shipments` | – | 200, lijst (elk object incl. `items`) |
| GET | `/shipments/{id}` | – | 200, object incl. `items` |
| GET | `/shipments/{id}/orders` | – | 200, lijst met max. 1 order-id, bv. `[1]` |
| GET | `/shipments/{id}/items` | – | 200, lijst `[{item_id, amount}]` |
| POST | `/shipments` | `POST_shipments.json` | 201, leeg |
| PUT | `/shipments/{id}` | `PUT_shipments_id.json` | 200, leeg |
| PUT | `/shipments/{id}/orders` | `PUT_shipments_id_orders.json` (**kale array van order-ids**) | 200, leeg |
| PUT | `/shipments/{id}/items` | `PUT_shipments_id_items.json` (**kale array**) | 200, leeg |
| DELETE | `/shipments/{id}` | – | altijd 403 |

Een shipment hoort bij precies één order (`order_id`). `PUT /shipments/{id}/orders` accepteert een lijst maar bewaart alleen het eerste id; een lege lijst `[]` maakt de koppeling leeg. `PUT /shipments/{id}/items` werkt `quantity_ordered` in de inventory bij (side effect!).

## Geldige waarden (uit de bestaande data)

| Veld | Waarden |
|---|---|
| `order_status` | `Pending`, `Shipped` |
| `transfer_status` | `Scheduled`, `Processed` |
| `shipment_type` | `Incoming`, `Outgoing` |
| `shipment_status` | `Scheduled`, `Transit`, `Delivered` |
| `shipping_method` | `Standard`, `Express`, `Freight`, `Overnight` |
| `payment_type` | `Automated`, `Manual` |
| `carrier_name` | `PostNL`, `GLS`, `DHL Parcel`, `DSV`, `Nabuurs`, `Simon Loos`, `Van den Bosch Transporten` |
| `packaging_type` (item) | `Case`, `Carton`, `Pallet`, `None` |

De legacy code valideert deze waarden **niet** — dit is wat er feitelijk in de data staat.

Bestaande id-ranges in de data (handig om botsingen te vermijden; voorbeelden gebruiken daarom `9001`): warehouses 1–10, locations 1–400, items 1–600, item_lines 1–14, item_groups 1–3, item_types 1–3, suppliers 1–21, clients 1–300, orders 1–4854, shipments 1–6132, transfers 1–800.

## Belangrijke legacy-quirks (essentieel voor de tests)

1. **De client levert zelf het `id` aan bij POST.** De server genereert géén id. Zonder `id` in de body crasht een transfer/order/shipment-POST zelfs (→ 500), en bij de rest verdwijnt het record in een staat waarin het niet meer op te vragen is.
2. **PUT is full replace, geen merge.** Het hele record wordt vervangen door jouw body (+ nieuwe `updated_at`). Stuur je `id` of `created_at` niet mee, dan zijn die velden daarna wég uit de data.
3. **Geen enkele validatie.** Verkeerde types, ontbrekende velden, extra velden, duplicate ids — alles wordt geaccepteerd. Een POST met een bestaand id maakt gewoon een tweede record aan; GET by id pakt daarna de eerste match.
4. **`created_at`/`updated_at` worden server-side gezet** bij POST; bij PUT alleen `updated_at`.
5. **GET van een onbestaand id geeft 200 met body `null`** (geen 404).
6. **Elke exception wordt een kale 500** (bv. niet-numeriek id in de URL, ongeldige JSON, PUT naar een niet-bestaand transfer-commit).
7. **PUT met `items`:** bij transfers/orders/shipments is `items` in de body optioneel; laat je hem weg dan blijven de bestaande regels staan, stuur je hem mee dan worden ze volledig vervangen.
8. **Side effects op inventory:** `PUT /orders/{id}/items` muteert `quantity_allocated`; `PUT /shipments/{id}/items` muteert `quantity_ordered`; `PUT /transfers/{id}/commit` muteert `quantity_on_hand` op twee locaties.
9. **Responses van POST/PUT/DELETE zijn leeg** — alleen een statuscode, nooit het aangemaakte/bijgewerkte object.
10. **DELETE verwijdert stilletjes niets** als het id niet bestaat: ook dan 200.
