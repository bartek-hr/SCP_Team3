"""Maakt risico-tabellen.html met risicoregister en 5x5-risicomatrix (te plakken in Word)."""
import html

# (id, categorie, risico, oorzaak/bewijs, gevolg, impact 1-5, kans 1-5, maatregel)
RISKS = [
    ("R01", "Betrouwbaarheid", "Wijzigingen gaan verloren",
     "fetch_*_pool() maakt per aanroep een nieuw object; save() schrijft een vers ingelezen kopie. Test: 0 van 11 resources slaat POST/PUT/DELETE op",
     "Voorraad, orders en zendingen kloppen niet met de werkelijkheid; misleveringen", 5, 5,
     "Eén datastore per request / database met transacties"),
    ("R02", "Beveiliging / beschikbaarheid", "Eén client kan de hele API platleggen",
     "Server is single-threaded en heeft geen timeout. Test: een onvolledige request (zonder API-key) blokkeert alle andere clients",
     "Magazijn ligt stil; ook zonder geldige key uit te voeren (DoS)", 5, 4,
     "Multithreaded server/framework, request-timeouts, reverse proxy"),
    ("R03", "Schaalbaarheid", "Zware requests blokkeren alle clients",
     "GET /orders duurt 130–205 s en GET /shipments 234 s (N+1: per order het hele itembestand lezen). Test: GET /warehouses (normaal 13 ms) wachtte 129,7 s tijdens een GET /orders",
     "Handscanners en werkstations wachten minuten; time-outs bij proxies", 4, 5,
     "Paginering, data één keer laden, database met JOIN, concurrency"),
    ("R04", "Beveiliging", "API-keys kunnen uitlekken",
     "Keys staan in platte tekst in user.json en in de Git-repository; geen verloopdatum of rotatie; voorspelbaar patroon (letters van de app-naam met cijfers)",
     "Iedereen met toegang tot de repo kan als facility_management alles wijzigen en verwijderen", 5, 3,
     "Keys gehasht opslaan, secrets buiten Git, rotatie, sterke willekeurige keys"),
    ("R05", "Beveiliging", "Verkeer kan worden afgeluisterd",
     "Alleen HTTP op poort 3000, geen TLS; API_KEY gaat leesbaar mee in elke request",
     "Key en bedrijfsdata te onderscheppen op het (wifi-)netwerk van het magazijn", 4, 3,
     "HTTPS via reverse proxy of Kestrel met certificaat"),
    ("R06", "Data-integriteit", "Ongeldige of dubbele data",
     "Geen invoervalidatie; client kiest zelf het id; geen uniciteit of verwijzingscontrole. Data bevat al 854 dubbele orderreferenties",
     "Corrupte data, verkeerde koppelingen, crashes bij latere verwerking", 4, 4,
     "Validatie, door database gegenereerde ids, UNIQUE- en FOREIGN KEY-constraints"),
    ("R07", "Concurrency", "Lost update bij gelijktijdige wijziging",
     "PUT vervangt het hele object; geen versienummer of ETag. Twee clients met dezelfde oude versie overschrijven elkaars wijziging (laatste wint)",
     "Wijziging van een collega verdwijnt ongemerkt (bv. voorraadcorrectie)", 3, 3,
     "Optimistic concurrency (rowversion/ETag + 409/412 Conflict), PATCH voor deelwijzigingen"),
    ("R08", "Betrouwbaarheid", "JSON-bestand corrupt bij crash",
     "save() opent het bestand met 'w' (eerst leegmaken) en schrijft dan; geen atomische write of back-up",
     "Bij stroomuitval of crash tijdens schrijven is een complete tabel weg", 5, 2,
     "Database met transacties, of schrijven naar tijdelijk bestand + rename"),
    ("R09", "Beheer", "Fouten en misbruik blijven onzichtbaar",
     "Elke exceptie wordt een lege 500 zonder logging; geen audit-log van wie wat wijzigt",
     "Problemen pas ontdekt door klachten; misbruik niet te herleiden", 3, 4,
     "Gestructureerde logging, audit-trail, monitoring"),
    ("R10", "Schaalbaarheid", "Groeit niet mee met de data",
     "Per request worden hele bestanden ingelezen en lineair doorzocht; data staat lokaal, dus geen tweede server mogelijk",
     "Responstijd groeit met elke order; geen horizontale schaling", 4, 4,
     "Database met indexen, stateless API achter load balancer"),
    ("R11", "Uitbreidbaarheid", "Uitbreiden is foutgevoelig",
     "Nieuwe resource vereist wijzigingen in 4 handlers in main.py (CC 62), data_provider, een nieuw model en user.json; veel duplicatie",
     "Regressies en hoge ontwikkelkosten bij elke uitbreiding", 3, 4,
     "Router/framework, generieke repository, lagenstructuur"),
    ("R12", "Testbaarheid", "Regressies worden niet opgemerkt",
     "Geen enkele geautomatiseerde test; geen CI-pipeline",
     "Fouten (zoals R01) komen in productie terecht", 4, 4,
     "Unit- en integratietests in CI-pipeline"),
    ("R13", "Beveiliging", "Raden van API-keys",
     "Geen rate limiting of blokkade na mislukte pogingen; voorspelbare keys",
     "Ongeautoriseerde toegang", 4, 2,
     "Rate limiting, lange willekeurige keys, monitoring op 401's"),
    ("R14", "Beveiliging", "Te grove autorisatie",
     "Rechten alleen per resource en methode; elke client ziet alle magazijnen, klanten en orders",
     "Filiaal- of klantdata zichtbaar voor apps die het niet nodig hebben", 3, 3,
     "Autorisatie per warehouse/klant (scopes, rollen)"),
    ("R15", "Supply chain", "Kwetsbare dependencies",
     "Alleen de Python-standaardbibliotheek; geen externe packages",
     "Laag risico; wel afhankelijk van Python-updates", 2, 1,
     "Python-versie actueel houden; bij rewrite dependency-scanning (Dependabot)"),
]


def level(score):
    return ("Kritiek", "#c00000", "#fff") if score >= 15 else ("Hoog", "#f4b183", "#000") if score >= 10 else (
        "Middel", "#ffe699", "#000") if score >= 5 else ("Laag", "#c6e0b4", "#000")


css = """body{font-family:Calibri,Arial,sans-serif;font-size:11pt;max-width:1150px;margin:24px auto;padding:0 16px;background:#fff;color:#000}
h1{font-size:16pt}h2{font-size:13pt;margin-top:28px}p.note{font-size:9.5pt;color:#444}
table{border-collapse:collapse;margin:8px 0}th,td{border:1px solid #999;padding:3px 6px;font-size:9.5pt;text-align:left;vertical-align:top}
th{background:#d9e2f3}td.n{text-align:center}
table.m td{width:120px;height:62px;text-align:center;vertical-align:middle;font-weight:bold;font-size:10pt}
table.m th{text-align:center;vertical-align:middle}"""
h = [f"<!doctype html><html lang='nl'><head><meta charset='utf-8'><title>Risico's legacy</title><style>{css}</style></head><body>",
     "<h1>Risico’s legacy CargoHUB API</h1>",
     "<p class='note'>Risico = Impact × Kans, beide op een schaal van 1 (zeer laag) tot 5 (zeer hoog). "
     "Kritiek ≥ 15, Hoog 10–14, Middel 5–9, Laag &lt; 5. Bewijs: <code>docs/endpoints-legacy/test_output.txt</code> en "
     "<code>docs/risicos-legacy/test_concurrency_output.txt</code>. Tip: selecteer een tabel, kopieer en plak in Word.</p>"]

h.append("<h2>Tabel 1 – Risicoregister (gesorteerd op score)</h2><table><tr><th>ID</th><th>Categorie</th><th>Risico</th>"
         "<th>Oorzaak / bewijs</th><th>Mogelijk gevolg</th><th>I</th><th>K</th><th>Score</th><th>Niveau</th><th>Maatregel (advies)</th></tr>")
for r in sorted(RISKS, key=lambda r: -r[5] * r[6]):
    rid, cat, name, why, eff, i, k, fix = r
    lv, bg, fg = level(i * k)
    h.append(f"<tr><td>{rid}</td><td>{cat}</td><td><b>{html.escape(name)}</b></td><td>{html.escape(why)}</td><td>{html.escape(eff)}</td>"
             f"<td class='n'>{i}</td><td class='n'>{k}</td><td class='n'><b>{i*k}</b></td>"
             f"<td style='background:{bg};color:{fg}'>{lv}</td><td>{html.escape(fix)}</td></tr>")
h.append("</table>")

IMP = {1: "Zeer laag", 2: "Laag", 3: "Middel", 4: "Hoog", 5: "Zeer hoog"}
h.append("<h2>Figuur – Risicomatrix</h2><table class='m'><tr><th rowspan='6' style='writing-mode:vertical-rl;transform:rotate(180deg)'>Kans</th></tr>")
for k in range(5, 0, -1):
    row = f"<tr><th>{k} – {IMP[k]}</th>"
    for i in range(1, 6):
        ids = [r[0] for r in RISKS if r[5] == i and r[6] == k]
        _, bg, fg = level(i * k)
        row += f"<td style='background:{bg};color:{fg}'>{'<br>'.join(ids)}</td>"
    h.append(row + "</tr>")
h.append("<tr><th></th><th></th>" + "".join(f"<th>{i} – {IMP[i]}</th>" for i in range(1, 6)) + "</tr>")
h.append("<tr><th></th><th></th><th colspan='5'>Impact</th></tr></table>")
h.append("</body></html>")
open("risico-tabellen.html", "w", encoding="utf-8").write("\n".join(h))
print("ok")
