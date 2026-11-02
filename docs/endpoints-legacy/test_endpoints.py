"""Test alle endpoints van de legacy CargoHUB API.

Draai de API eerst (op een KOPIE van legacy/, want POST/PUT/DELETE schrijven naar data/):
    cd <kopie>/api && python main.py
Daarna:  python test_endpoints.py <pad-naar-kopie>/data/user.json
Schrijft resultaten naar results.json en drukt een tabel af.
"""
import http.client
import json
import sys
import time

HOST, PORT = "127.0.0.1", 3000
users = {u["app"]: u["api_key"] for u in json.load(open(sys.argv[1], encoding="utf-8"))}
READ = users["analytics_dashboard"]          # alleen GET
FACILITY = users["facility_management"]      # POST/PUT/DELETE op stamdata
RECEIVING = users["receiving_station"]       # POST/PUT op items, orders, shipments, transfers, inventories
SHIPPING = users["shipping_station"]         # POST/PUT op clients
results = []


def call(method, path, key=READ, body=None, raw=None, timeout=300):
    conn = http.client.HTTPConnection(HOST, PORT, timeout=timeout)
    headers = {}
    if key is not None:
        headers["API_KEY"] = key
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    if data is not None:
        headers["Content-Length"] = str(len(data))
    start = time.perf_counter()
    try:
        conn.request(method, path, body=data, headers=headers)
        resp = conn.getresponse()
        payload = resp.read()
        ms = (time.perf_counter() - start) * 1000
        ctype = resp.getheader("Content-type")
        return resp.status, payload, ms, ctype
    except Exception as e:  # geen antwoord / verbinding verbroken
        return f"geen antwoord ({type(e).__name__})", b"", (time.perf_counter() - start) * 1000, None
    finally:
        conn.close()


def record(group, method, path, desc, expect, key=READ, body=None, raw=None, check=None):
    status, payload, ms, ctype = call(method, path, key, body, raw)
    try:
        parsed = json.loads(payload) if payload else None
    except ValueError:
        parsed = "<geen JSON>"
    summary = ("lijst van %d" % len(parsed)) if isinstance(parsed, list) else (
        "null" if parsed is None and payload else ("object" if isinstance(parsed, dict) else ("leeg" if not payload else str(parsed)[:40])))
    ok = status == expect
    note = ""
    if check is not None:
        ok2, note = check(parsed)
        ok = ok and ok2
    results.append(dict(group=group, method=method, path=path, desc=desc, expect=expect, status=status,
                        ms=round(ms), body=summary, ctype=ctype, ok=ok, note=note))
    print(f"{'OK ' if ok else 'FOUT'} {method:6} {path:45} verwacht {expect} kreeg {status} ({ms:.0f} ms, {summary}) {note}")
    return parsed


V = "/api/v1"
# ---------- 1. GET: alle lees-endpoints met een bestaand id ----------
for path, desc in [
    ("/warehouses", "Alle warehouses"), ("/warehouses/1", "Eén warehouse"), ("/warehouses/1/locations", "Locaties in warehouse"),
    ("/locations", "Alle locaties"), ("/locations/1", "Eén locatie"),
    ("/transfers", "Alle transfers (incl. items)"), ("/transfers/1", "Eén transfer"), ("/transfers/1/items", "Items in transfer"),
    ("/items", "Alle items"), ("/items/1", "Eén item"), ("/items/1/inventory", "Voorraadregels van item"),
    ("/items/1/inventory/totals", "Voorraadtotalen van item"),
    ("/item_lines", "Alle item lines"), ("/item_lines/1", "Eén item line"), ("/item_lines/1/items", "Item-ids in item line"),
    ("/item_groups", "Alle item groups"), ("/item_groups/1", "Eén item group"), ("/item_groups/1/items", "Item-ids in item group"),
    ("/item_types", "Alle item types"), ("/item_types/1", "Eén item type"), ("/item_types/1/items", "Item-ids in item type"),
    ("/inventories", "Alle voorraadregels"), ("/inventories/1", "Eén voorraadregel (bewust 404: samengestelde sleutel)"),
    ("/suppliers", "Alle leveranciers"), ("/suppliers/1", "Eén leverancier"), ("/suppliers/1/items", "Items van leverancier"),
    ("/orders", "Alle orders (incl. items)"), ("/orders/1", "Eén order"), ("/orders/1/items", "Items in order"),
    ("/clients", "Alle klanten"), ("/clients/1", "Eén klant"), ("/clients/1/orders", "Orders van klant"),
    ("/shipments", "Alle zendingen (incl. items)"), ("/shipments/1", "Eén zending"),
    ("/shipments/1/orders", "Order-id van zending"), ("/shipments/1/items", "Items in zending"),
]:
    record("GET", "GET", V + path, desc, 404 if path == "/inventories/1" else 200)

# ---------- 2. Foutgevallen ----------
record("Fout", "GET", V + "/warehouses/99999", "Niet-bestaand id", 404)
record("Fout", "GET", V + "/warehouses/abc", "Ongeldig id (tekst)", 400)
record("Fout", "GET", V + "/warehouses/", "Afsluitende slash", 200)
record("Fout", "GET", V + "/warehouses?limit=5", "Query-parameter", 200)
record("Fout", "GET", V + "/bestaatniet", "Onbekende resource", 404)
record("Fout", "GET", "/api/v2/warehouses", "Onbekende API-versie", 404)
record("Fout", "GET", "/", "Root-URL", 404)
record("Fout", "GET", V + "/warehouses", "Zonder API-key", 401, key=None)
record("Fout", "GET", V + "/warehouses", "Ongeldige API-key", 401, key="fout")
record("Fout", "POST", V + "/warehouses", "Key zonder schrijfrecht", 403, key=READ, body={"id": 1})
record("Fout", "POST", V + "/warehouses", "Ongeldige JSON in body", 400, key=FACILITY, raw=b"{niet json")
record("Fout", "PUT", V + "/warehouses/99999", "PUT op niet-bestaand id", 404, key=FACILITY, body={"id": 99999, "name": "x"})
record("Fout", "DELETE", V + "/warehouses/99999", "DELETE op niet-bestaand id", 404, key=FACILITY)
record("Fout", "PATCH", V + "/warehouses/1", "Niet-ondersteunde methode", 405, key=FACILITY, body={})

# ---------- 3. Schrijven + nacontrole (wordt het opgeslagen?) ----------
exists = lambda p: (p is not None and p != "<geen JSON>", "opgeslagen" if p else "NIET opgeslagen (GET geeft null)")
gone = lambda p: (p is None, "verwijderd" if p is None else "NIET verwijderd")
NEW = 90001
for res, key, body, upd in [
    ("warehouses", FACILITY, {"id": NEW, "code": "TST", "name": "Test"}, {"id": NEW, "code": "TST", "name": "Gewijzigd"}),
    ("locations", FACILITY, {"id": NEW, "warehouse_id": 1, "code": "T-1", "name": "Test"}, {"id": NEW, "warehouse_id": 1, "code": "T-1", "name": "Gewijzigd"}),
    ("items", RECEIVING, {"id": NEW, "code": "ITM-T", "description": "Test"}, {"id": NEW, "code": "ITM-T", "description": "Gewijzigd"}),
    ("item_lines", FACILITY, {"id": NEW, "name": "Test"}, {"id": NEW, "name": "Gewijzigd"}),
    ("item_groups", FACILITY, {"id": NEW, "name": "Test"}, {"id": NEW, "name": "Gewijzigd"}),
    ("item_types", FACILITY, {"id": NEW, "name": "Test"}, {"id": NEW, "name": "Gewijzigd"}),
    ("suppliers", FACILITY, {"id": NEW, "code": "SUP-T", "name": "Test"}, {"id": NEW, "code": "SUP-T", "name": "Gewijzigd"}),
    ("clients", FACILITY, {"id": NEW, "name": "Test"}, {"id": NEW, "name": "Gewijzigd"}),
    ("orders", RECEIVING, {"id": NEW, "client_id": 1, "items": [{"item_id": 1, "amount": 1}]}, {"id": NEW, "client_id": 1}),
    ("shipments", RECEIVING, {"id": NEW, "order_id": 1, "items": [{"item_id": 1, "amount": 1}]}, {"id": NEW, "order_id": 1}),
    ("transfers", RECEIVING, {"id": NEW, "from_location_id": 1, "to_location_id": 2, "items": [{"item_id": 1, "amount": 1}]}, {"id": NEW, "from_location_id": 1, "to_location_id": 2}),
]:
    record("Schrijven", "POST", f"{V}/{res}", f"Nieuw {res[:-1]} aanmaken", 201, key=key, body=body)
    record("Schrijven", "GET", f"{V}/{res}/{NEW}", "Controle na POST", 200, check=exists)
    record("Schrijven", "PUT", f"{V}/{res}/1", f"Bestaand {res[:-1]} wijzigen", 200, key=key if res not in ("clients",) else SHIPPING,
           body=dict(upd, id=1))
    after = call("GET", f"{V}/{res}/1")[1]
    changed = b"Gewijzigd" in after or (res in ("orders", "shipments", "transfers"))
    results[-1]["note"] = "wijziging opgeslagen" if b"Gewijzigd" in after else ("niet te controleren" if res in ("orders", "shipments", "transfers") else "wijziging NIET opgeslagen")
    results[-1]["ok"] = results[-1]["ok"] and changed
    print("      controle na PUT:", results[-1]["note"])
    record("Schrijven", "DELETE", f"{V}/{res}/2", f"{res[:-1]} verwijderen", 200, key=FACILITY)
    record("Schrijven", "GET", f"{V}/{res}/2", "Controle na DELETE", 404, check=gone)

record("Schrijven", "POST", f"{V}/inventories", "Voorraadregel aanmaken", 201, key=RECEIVING,
       body={"item_id": 1, "location_id": 1, "quantity_on_hand": 5})
record("Schrijven", "PUT", f"{V}/orders/1/items", "Items van order vervangen", 200, key=RECEIVING, body=[{"item_id": 1, "amount": 2}])
record("Schrijven", "PUT", f"{V}/shipments/1/items", "Items van zending vervangen", 200, key=RECEIVING, body=[{"item_id": 1, "amount": 2}])
record("Schrijven", "PUT", f"{V}/shipments/1/orders", "Order van zending wijzigen", 200, key=RECEIVING, body=[2])
before = call("GET", f"{V}/items/{1}/inventory")[1]
record("Schrijven", "PUT", f"{V}/transfers/1/commit", "Transfer verwerken (voorraad verplaatsen)", 200, key=RECEIVING)
record("Schrijven", "GET", f"{V}/transfers/1", "Controle status na commit", 200,
       check=lambda p: (isinstance(p, dict) and p.get("transfer_status") == "Processed", f"status = {p.get('transfer_status') if isinstance(p, dict) else p}"))
record("Schrijven", "PUT", f"{V}/inventories/1", "PUT voorraadregel (bewust 404)", 404, key=RECEIVING, body={})
record("Schrijven", "DELETE", f"{V}/inventories/1", "DELETE voorraadregel (bewust 404)", 404, key=FACILITY)

json.dump(results, open("results.json", "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print(f"\n{sum(r['ok'] for r in results)}/{len(results)} gevallen gedragen zich zoals verwacht")
