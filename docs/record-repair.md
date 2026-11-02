# Corrupte records herstellen

De beheeropdracht controleert een bestaande SQLite-database en toont een JSON-rapport.
Zonder `--apply` verandert er niets. Gebruik een databasekopie en stop andere schrijvers voordat je herstel toepast.

```bash
dotnet build CargoHUB.csproj
dotnet bin/Debug/net10.0/CargoHUB.dll --repair-records /pad/naar/kopie.sqlite
# Herstel toepassen:
dotnet bin/Debug/net10.0/CargoHUB.dll --repair-records /pad/naar/kopie.sqlite --apply
```

Automatisch herstel verwijdert alleen omringende witruimte uit naam, omschrijving,
adres, plaats, provincie, land en contactnaam. Ontbrekende waarden of verwijzingen,
ongeldige datums, e-mailadressen, statussen en ongeldige of negatieve aantallen blijven
ongewijzigd als `NeedsReview`. Bij zo'n probleem wordt het hele record overgeslagen.
Nummers, koppelingen en voorraad worden nooit geraden; records worden niet verwijderd.
De PO moet deze voorlopige herstelregels nog bevestigen.

Bij toepassen bewaart `RecordRepairRuns` de reden, uitkomst en oorspronkelijke en
nieuwe waarden. Herstel en registratie slagen samen of worden samen teruggedraaid.
`NeedsReview` blijft beschikbaar voor latere beoordeling en archivering (#85).
Exitcodes: `0` voltooid, `3` beoordeling nodig, `2` ongeldige argumenten, `1` fout.
