# Shipments

Bron: [shipments.py](../../../../legacy/api/models/shipments.py)

## Beschrijving

Zendingen, inkomend of uitgaand. Met status, vervoerder en bij welke order het hoort.

## taken
- alle zendingen ophalen
- één zending ophalen
- items in een zending ophalen
- orders bij een zending ophalen
- een zending toevoegen
- een zending aanpassen
- items in een zending updaten
- orders bij een zending updaten
- een zending verwijderen

## Opmerkingen

Een zending hoort bij één order. Als de items veranderen, wordt ook de bestelde voorraad bijgewerkt.
