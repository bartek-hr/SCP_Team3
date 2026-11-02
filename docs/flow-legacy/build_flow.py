"""Maakt flow-tabellen.html (te plakken in Word): use cases -> endpoints en clients -> endpoints.

Gebruik: python build_flow.py ../../legacy/data/user.json
"""
import html
import json
import sys

users = {u["app"]: u["endpoint_access"] for u in json.load(open(sys.argv[1], encoding="utf-8"))}
CLIENTS = [  # (app, type client, gebruiker)
    ("analytics_dashboard", "Dashboard (web)", "Management / planning"),
    ("order_picker", "Handscanner", "Orderpicker"),
    ("smartglass_reader", "Smart glasses", "Magazijnmedewerker"),
    ("receiving_station", "Werkstation ontvangst", "Medewerker goederenontvangst"),
    ("shipping_station", "Werkstation expeditie", "Medewerker expeditie"),
    ("facility_management", "Beheerportaal (web)", "Facility manager"),
]
RES = ["warehouses", "locations", "transfers", "items", "item_lines", "item_groups", "item_types",
       "inventories", "suppliers", "orders", "clients", "shipments"]


def allowed(app, method, path):
    return users[app].get(path.split("/")[1].split("?")[0], {}).get(method.lower(), False)


# (id, naam, primaire client, stappen: (methode, pad, uitleg))
UC = [
    ("UC-01", "Magazijn inrichten", "facility_management", [
        ("POST", "/warehouses", "warehouse aanmaken"),
        ("POST", "/locations", "per vak/stelling een locatie met warehouse_id"),
        ("GET", "/warehouses/{id}/locations", "controleren of alle locaties er staan")]),
    ("UC-02", "Artikel en leverancier registreren", "receiving_station", [
        ("GET", "/suppliers/{id}", "bestaat de leverancier?"),
        ("POST", "/suppliers", "zo niet: leverancier aanmaken"),
        ("GET", "/item_lines", "classificatie opzoeken (ook item_groups, item_types)"),
        ("POST", "/items", "artikel aanmaken met supplier_id en classificatie")]),
    ("UC-03", "Goederen ontvangen", "receiving_station", [
        ("POST", "/shipments", "inkomende zending vastleggen (shipment_type Incoming)"),
        ("PUT", "/shipments/{id}/items", "ontvangen artikelen en aantallen"),
        ("GET", "/items/{id}/inventory", "huidige voorraadregels bekijken"),
        ("POST", "/inventories", "voorraad op ontvangstlocatie ophogen (upsert)")]),
    ("UC-04", "Order aannemen", "receiving_station", [
        ("GET", "/clients/{id}", "klant (filiaal) opzoeken"),
        ("GET", "/items/{id}/inventory/totals", "is er genoeg beschikbaar?"),
        ("POST", "/orders", "order met orderregels aanmaken"),
        ("PUT", "/orders/{id}/items", "regels wijzigen: quantity_allocated wordt bijgewerkt")]),
    ("UC-05", "Order picken", "order_picker", [
        ("GET", "/orders/{id}", "order incl. items ophalen"),
        ("GET", "/items/{id}/inventory", "op welke locaties ligt het artikel?"),
        ("GET", "/locations/{id}", "locatiecode tonen op scanner"),
        ("PUT", "/orders/{id}", "status bijwerken na picken")]),
    ("UC-06", "Order verzenden", "shipping_station", [
        ("GET", "/orders/{id}", "gepickte order ophalen"),
        ("POST", "/shipments", "uitgaande zending aanmaken met vervoerder"),
        ("PUT", "/shipments/{id}/orders", "zending aan order koppelen"),
        ("PUT", "/shipments/{id}/items", "artikelen in zending: quantity_ordered bijgewerkt"),
        ("PUT", "/orders/{id}", "orderstatus naar Shipped")]),
    ("UC-07", "Voorraad verplaatsen (transfer)", "smartglass_reader", [
        ("GET", "/items/{id}/inventory", "bron- en doellocatie bepalen"),
        ("POST", "/transfers", "transfer plannen (status Scheduled)"),
        ("GET", "/transfers/{id}", "taak tonen aan medewerker"),
        ("PUT", "/transfers/{id}/commit", "uitvoeren: voorraad van bron naar doel, status Processed")]),
    ("UC-08", "Voorraad corrigeren na telling", "smartglass_reader", [
        ("GET", "/items/{id}/inventory", "geregistreerde voorraad tonen"),
        ("PUT", "/inventories/{id}", "getelde hoeveelheid opslaan")]),
    ("UC-09", "Klant (filiaal) beheren", "shipping_station", [
        ("POST", "/clients", "nieuw filiaal aanmaken"),
        ("PUT", "/clients/{id}", "adres of contact wijzigen"),
        ("GET", "/clients/{id}/orders", "orderhistorie van filiaal")]),
    ("UC-10", "Rapportage", "analytics_dashboard", [
        ("GET", "/warehouses", "overzicht magazijnen"),
        ("GET", "/inventories", "alle voorraadregels"),
        ("GET", "/orders", "alle orders"),
        ("GET", "/shipments", "alle zendingen")]),
]
# bekende problemen uit de endpointtests (4.3)
ISSUE = {
    ("PUT", "/inventories/{id}"): "geeft altijd 404",
    ("GET", "/orders"): "duurt 205 s",
    ("GET", "/shipments"): "duurt 234 s",
    ("PUT", "/transfers/{id}/commit"): "200, maar niets opgeslagen",
}

css = """body{font-family:Calibri,Arial,sans-serif;font-size:11pt;max-width:1150px;margin:24px auto;padding:0 16px;background:#fff;color:#000}
h1{font-size:16pt}h2{font-size:13pt;margin-top:28px}p.note{font-size:9.5pt;color:#444}
table{border-collapse:collapse;margin:8px 0}th,td{border:1px solid #999;padding:3px 6px;font-size:9.5pt;text-align:left;vertical-align:top}
th{background:#d9e2f3}code{font-family:Consolas,monospace;font-size:9pt}td.c{text-align:center}
td.nee{background:#f8d7da}td.ja{background:#d4edda}td.leeg{background:#f2f2f2}"""
h = [f"<!doctype html><html lang='nl'><head><meta charset='utf-8'><title>Flow legacy</title><style>{css}</style></head><body>",
     "<h1>Flow legacy CargoHUB API – use cases en clients</h1>",
     "<p class='note'>Afgeleid uit <code>legacy/api/main.py</code> en <code>legacy/data/user.json</code> (commit 0519310). "
     "Alle paden beginnen met <code>/api/v1</code>. Tip: selecteer een tabel, kopieer en plak in Word.</p>"]

h.append("<h2>Tabel 1 – Use cases en de endpoints in volgorde</h2><table><tr><th>Use case</th><th>Client (actor)</th>"
         "<th>Stap</th><th>Endpoint</th><th>Doel van de stap</th><th>Mag deze client dit?</th></tr>")
for uid, name, app, steps in UC:
    for i, (m, p, why) in enumerate(steps, 1):
        ok = allowed(app, m, p)
        issue = ISSUE.get((m, p))
        cell = ("ja" if ok else "nee – 403") + (f"; {issue}" if issue else "")
        cls = "nee" if (not ok or issue) else "ja"
        first = f"<td rowspan='{len(steps)}'><b>{uid}</b><br>{html.escape(name)}</td><td rowspan='{len(steps)}'><code>{app}</code></td>" if i == 1 else ""
        h.append(f"<tr>{first}<td class='c'>{i}</td><td><code>{m} {html.escape(p)}</code></td><td>{html.escape(why)}</td><td class='{cls}'>{cell}</td></tr>")
h.append("</table><p class='note'>Rood = de client kan deze stap niet uitvoeren (geen recht) of het endpoint werkt niet (zie 4.3).</p>")

h.append("<h2>Tabel 2 – Type clients</h2><table><tr><th>Type client</th><th>App (API-key)</th><th>Gebruiker</th><th>Gebruik</th></tr>")
for app, typ, who in CLIENTS:
    writes = sorted({r for r, ms in users[app].items() for m, v in ms.items() if v and m != "get"})
    use = "alleen lezen" if not writes else "lezen + schrijven op " + ", ".join(writes)
    h.append(f"<tr><td>{typ}</td><td><code>{app}</code></td><td>{who}</td><td>{html.escape(use)}</td></tr>")
h.append("</table>")

h.append("<h2>Tabel 3 – Welke client mag welke endpoints gebruiken</h2><table><tr><th>Resource</th>"
         + "".join(f"<th>{typ}<br><code>{app}</code></th>" for app, typ, _ in CLIENTS) + "</tr>")
for r in RES:
    row = f"<tr><td><code>/{r}</code></td>"
    for app, _, _ in CLIENTS:
        ms = users[app].get(r, {})
        letters = "".join(l for m, l in (("get", "G"), ("post", "P"), ("put", "U"), ("delete", "D")) if ms.get(m))
        row += f"<td class='c{'' if letters != 'G' else ''}'>{letters or '–'}</td>"
    h.append(row + "</tr>")
h.append("</table><p class='note'>G = GET (lezen), P = POST (aanmaken), U = PUT (wijzigen), D = DELETE (verwijderen). "
         "GET geeft toegang tot alle GET-endpoints van die resource, inclusief sub-paden zoals <code>/items/{id}/inventory</code>.</p>")

h.append("<h2>Tabel 4 – Opvallend in de flows</h2><table><tr><th>Bevinding</th><th>Gevolg</th></tr>")
for f, g in [
    ("<code>smartglass_reader</code> en <code>order_picker</code> hebben geen POST op transfers",
     "Ze kunnen een transfer alleen uitvoeren (commit), niet zelf plannen; dat moet via een werkstation (UC-07)"),
    ("<code>PUT /inventories/{id}</code> geeft altijd 404", "Voorraadcorrectie na telling (UC-08) is voor geen enkele client mogelijk"),
    ("Geen client heeft DELETE op items, orders, shipments, transfers, inventories", "Foutieve orders of zendingen kunnen niet worden verwijderd"),
    ("<code>receiving_station</code> mag orders aanmaken, <code>shipping_station</code> niet", "Orderinvoer ligt bij de ontvangst, wat procesmatig onlogisch lijkt"),
    ("De server verwerkt requests één voor één (<code>socketserver.TCPServer</code>, niet multithreaded)",
     "Eén <code>GET /orders</code> van het dashboard (205 s) blokkeert alle handscanners en werkstations"),
]:
    h.append(f"<tr><td>{f}</td><td>{html.escape(g)}</td></tr>")
h.append("</table></body></html>")
open("flow-tabellen.html", "w", encoding="utf-8").write("\n".join(h))
print("ok")
