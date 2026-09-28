# Requirements & ontwerpuitgangspunten

Deze notities beschrijven de doelen van de C#-rewrite. Ze zijn geen overzicht van reeds opgeleverde functionaliteit. De oorspronkelijke README-notities zijn hier samengebracht.

## Compatibiliteit

- Behoud de bestaande API-endpoints zodat aangesloten clients en frontends kunnen blijven werken.
- Documenteer en test het huidige gedrag als referentie voor de nieuwe implementatie.

## Data & toegang

- Lever correcte gegevens met passende rechten voor de verschillende magazijnen en supermarkten.
- Voorkom databasecorruptie door invoer te valideren, fouten af te handelen en het gedrag te testen.
- Zorg voor gebruikersbeheer en authenticatie zodat alleen bevoegde gebruikers gegevens kunnen wijzigen.
- Ondersteun het volgen van goederen vanaf de fabriek of leverancier tot het eindpunt in de supermarkt.

## Kwaliteit & volgorde

- Streef naar betere prestaties dan de bestaande API.
- Bouw eerst een betrouwbare basis voordat nieuwe functionaliteit wordt toegevoegd.

## Testaanpak

- Schrijf tests in C# waarmee zowel de oude Python-endpoints als de nieuwe C#-endpoints kunnen worden gecontroleerd.
- Voeg daarnaast tests toe voor de interne werking van de nieuwe C#-code.
- Voor de interne Python-code zijn geen nieuwe tests gepland; het waarneembare API-gedrag blijft wel onderdeel van de vergelijking.
