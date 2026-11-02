"""Maakt endpoints-tabellen.html (te plakken in Word) uit results.json."""
import html
import json
import sys

R = json.load(open("results.json", encoding="utf-8"))
users = json.load(open(sys.argv[1], encoding="utf-8"))  # pad naar data/user.json
res = {}
for r in R:
    if r["group"] != "Fout":  # foutgevallen staan apart in tabel 2
        res.setdefault((r["method"], r["path"].replace("/api/v1", "")), r)


def t(m, p):
    r = res.get((m, p))
    if not r:
        return ("–", "", "")
    note = r.get("note") or r["body"]
    if m == "POST" and p != "/inventories":  # resultaat = GET op het nieuwe id
        check = res.get(("GET", f"{p}/90001"))
        note = check["note"] if check else note
    if m == "DELETE" and r["status"] == 200:
        check = next((x for x in R if x["method"] == "GET" and x["path"] == "/api/v1" + p and x["note"]), None)
        note = check["note"] if check else note
    return (str(r["status"]), f"{r['ms']:,}".replace(",", "."), note)


def who(m, p):
    resource = p.split("/")[1]
    n = [u["app"] for u in users if u["endpoint_access"].get(resource, {}).get(m.lower())]
    return "geen enkele" if not n else ("alle 6" if len(n) == 6 else ", ".join(n))


E = []  # (methode, pad, omschrijving, getest pad)
for name, nl, sub in [
    ("warehouses", "warehouse", [("locations", "Alle locaties in dit warehouse")]),
    ("locations", "locatie", []),
    ("transfers", "transfer", [("items", "Items (item_id + aantal) in deze transfer")]),
    ("items", "item", [("inventory", "Voorraadregels van dit item per locatie"),
                       ("inventory/totals", "Totalen: verwacht, besteld, gereserveerd, beschikbaar")]),
    ("item_lines", "item line", [("items", "Ids van items in deze item line")]),
    ("item_groups", "item group", [("items", "Ids van items in deze item group")]),
    ("item_types", "item type", [("items", "Ids van items in dit item type")]),
    ("inventories", "voorraadregel", []),
    ("suppliers", "leverancier", [("items", "Items van deze leverancier")]),
    ("orders", "order", [("items", "Items (item_id + aantal) in deze order")]),
    ("clients", "klant", [("orders", "Orders waarin deze klant klant, ship-to of bill-to is")]),
    ("shipments", "zending", [("orders", "Order-id van deze zending (als lijst)"),
                              ("items", "Items (item_id + aantal) in deze zending")]),
]:
    incl = " incl. items" if name in ("orders", "shipments", "transfers") else ""
    E.append(("GET", f"/{name}", f"Lijst van alle {nl}s{incl}", f"/{name}"))
    E.append(("GET", f"/{name}/{{id}}", "Altijd 404 (voorraad heeft samengestelde sleutel)" if name == "inventories" else f"Eén {nl} op id", f"/{name}/1"))
    for s, d in sub:
        E.append(("GET", f"/{name}/{{id}}/{s}", d, f"/{name}/1/{s}"))
    extra = " (upsert op item_id + location_id)" if name == "inventories" else (", status wordt Scheduled + notificatie" if name == "transfers" else "")
    E.append(("POST", f"/{name}", f"Nieuwe {nl} toevoegen{extra}", f"/{name}"))
    E.append(("PUT", f"/{name}/{{id}}", "Altijd 404" if name == "inventories" else f"{nl.capitalize()} vervangen", f"/{name}/1"))
    if name == "transfers":
        E.append(("PUT", "/transfers/{id}/commit", "Transfer uitvoeren: voorraad van → naar locatie, status Processed", "/transfers/1/commit"))
    if name == "orders":
        E.append(("PUT", "/orders/{id}/items", "Items van order vervangen en quantity_allocated bijwerken", "/orders/1/items"))
    if name == "shipments":
        E.append(("PUT", "/shipments/{id}/orders", "Order van zending wijzigen (eerste id uit lijst)", "/shipments/1/orders"))
        E.append(("PUT", "/shipments/{id}/items", "Items van zending vervangen en quantity_ordered bijwerken", "/shipments/1/items"))
    E.append(("DELETE", f"/{name}/{{id}}", "Altijd 404" if name == "inventories" else f"{nl.capitalize()} verwijderen",
              f"/{name}/{1 if name == 'inventories' else 2}"))

css = """body{font-family:Calibri,Arial,sans-serif;font-size:11pt;max-width:1150px;margin:24px auto;padding:0 16px;background:#fff;color:#000}
h1{font-size:16pt}h2{font-size:13pt;margin-top:28px}p.note{font-size:9.5pt;color:#444}
table{border-collapse:collapse;margin:8px 0}th,td{border:1px solid #999;padding:3px 6px;font-size:9.5pt;text-align:left;vertical-align:top}
th{background:#d9e2f3}td.n{text-align:right}tr.bad td{background:#f8d7da}tr.warn td{background:#fff3cd}code{font-family:Consolas,monospace;font-size:9pt}"""
h = [f"<!doctype html><html><head><meta charset='utf-8'><title>Endpoints legacy</title><style>{css}</style></head><body>",
     "<h1>Endpoints legacy CargoHUB API – overzicht en testresultaten</h1>",
     "<p class='note'>Getest op 4 oktober 2026 tegen een kopie van <code>legacy/</code> (commit 0519310), Python 3.14, lokaal op 127.0.0.1:3000. "
     "Script: <code>test_endpoints.py</code>, ruwe uitvoer: <code>test_output.txt</code>. Alle paden beginnen met <code>/api/v1</code>; "
     "elke request vereist de header <code>API_KEY</code>. Tip: selecteer een tabel, kopieer en plak in Word.</p>"]

h.append(f"<h2>Tabel 1 – Alle endpoints ({len(E)})</h2><table><tr><th>#</th><th>Methode</th><th>Pad</th><th>Wat doet het</th>"
         "<th>Recht (apps)</th><th>Status</th><th>Tijd (ms)</th><th>Resultaat</th></tr>")
for i, (m, p, d, tp) in enumerate(E, 1):
    st, ms, note = t(m, tp)
    slow = ms and int(ms.replace(".", "")) > 1000
    cls = "bad" if ("NIET" in note or (st == "403" and m == "DELETE")) else ("warn" if slow else "")
    if st == "403" and m == "DELETE":
        note = "geen enkele app mag dit"
    h.append(f"<tr class='{cls}'><td class='n'>{i}</td><td>{m}</td><td><code>{html.escape(p)}</code></td><td>{html.escape(d)}</td>"
             f"<td>{who(m, p)}</td><td>{st}</td><td class='n'>{ms}</td><td>{html.escape(note)}</td></tr>")
h.append("</table><p class='note'>Rood = werkt niet zoals bedoeld; geel = trager dan 1 seconde. Bij POST/PUT/DELETE is "
         "‘Resultaat’ de controle met een GET achteraf (zie tabel 3).</p>")

h.append("<h2>Tabel 2 – Foutafhandeling</h2><table><tr><th>Situatie</th><th>Request</th><th>Verwacht (REST-conventie)</th><th>Kreeg</th></tr>")
for r in [x for x in R if x["group"] == "Fout"]:
    got = html.escape(str(r["status"])) + (" (body: null)" if r["body"] == "null" else "")
    h.append(f"<tr class='{'' if r['ok'] else 'bad'}'><td>{html.escape(r['desc'])}</td><td><code>{r['method']} {html.escape(r['path'])}</code></td>"
             f"<td>{r['expect']}</td><td>{got}</td></tr>")
h.append("</table>")


def find(method, path):
    return next((r for r in R if r["group"] == "Schrijven" and r["method"] == method and r["path"] == path), None)


h.append("<h2>Tabel 3 – Worden wijzigingen echt opgeslagen?</h2><table><tr><th>Resource</th><th>POST → GET nieuw id</th>"
         "<th>PUT → GET</th><th>DELETE → GET</th></tr>")
for name in ["warehouses", "locations", "items", "item_lines", "item_groups", "item_types", "suppliers", "clients", "orders", "shipments", "transfers"]:
    post, put = find("GET", f"/api/v1/{name}/90001"), find("PUT", f"/api/v1/{name}/1")
    dl, dg = find("DELETE", f"/api/v1/{name}/2"), find("GET", f"/api/v1/{name}/2")
    dtxt = "403 – geen enkele app heeft DELETE-recht" if dl["status"] == 403 else f"200, maar {dg['note']}"
    h.append(f"<tr class='bad'><td>{name}</td><td>201, maar {post['note']}</td><td>200, {put['note']}</td><td>{dtxt}</td></tr>")
c = next(r for r in R if r["group"] == "Schrijven" and r["method"] == "GET" and r["path"] == "/api/v1/transfers/1" and "status" in r["note"])
h.append(f"<tr class='bad'><td>transfers/1/commit</td><td colspan='3'>200, maar daarna {c['note']} (verwacht Processed)</td></tr></table>")

h.append("<h2>Tabel 4 – Gebruikte HTTP-statuscodes</h2><table><tr><th>Code</th><th>Wanneer de legacy-API hem geeft</th><th>Correct gebruik?</th></tr>")
for code, when, ok in [
    ("200 OK", "Elke geslaagde GET, PUT en DELETE – ook als het id niet bestaat", "Deels: onbekend id hoort 404 te geven"),
    ("201 Created", "Elke POST", "Deels: zonder body of Location-header, en ook als er niets is opgeslagen"),
    ("401 Unauthorized", "API_KEY ontbreekt of is onbekend", "Ja"),
    ("403 Forbidden", "App heeft geen recht op resource/methode – óók bij onbekende resource of query-string", "Deels: onbekende resource hoort 404"),
    ("404 Not Found", "Onbekend sub-pad, inventories/{id}", "Te weinig gebruikt"),
    ("500 Internal Server Error", "Elke exceptie: tekst als id, afsluitende slash, ongeldige JSON", "Nee: invoerfouten horen 400"),
    ("501 Not Implemented", "PATCH en andere methodes (standaard van http.server, HTML-body)", "Hoort 405 Method Not Allowed"),
    ("(geen antwoord)", "Pad zonder /api/v1 – verbinding wordt gesloten zonder response", "Nee: hoort 404"),
]:
    h.append(f"<tr><td>{code}</td><td>{html.escape(when)}</td><td>{html.escape(ok)}</td></tr>")
h.append(f"</table><p class='note'>Totaal: {sum(r['ok'] for r in R)} van {len(R)} testgevallen gedroegen zich zoals verwacht.</p></body></html>")
open("endpoints-tabellen.html", "w", encoding="utf-8").write("\n".join(h))
print(len(E), "endpoints")
