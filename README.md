Zaklád bakalárskej práce 

Zatiaľ oužité technológie 

.NET 9
ASP.NET Core (Blazor Server)
Entity Framework Core 9
SQLite
C#

Štruktura projektu
WeaponShop.Domain – doménové entity (napr. Weapon)
WeaponShop.Application – aplikačná logika, služby
WeaponShop.Infrastructure – databáza, EF Core kontext, repozitáre
WeaponShop.Web – Blazor Server webová vrstva

Funkcie (aktuálny stav)
evidencia zbraní
jednoduché CRUD operácie
SQLite databáza
základné seedovacie dáta (rozne zbrane..)

oddelenie vrstiev na spôsob Clean Architecture 

Na zaklade nasho rozhovoru toto budem doplnovat nasledujuce tyzdne - 
evidencia zákazníkov

objednávky a faktúry
možnosť generovať PDF účtenky
e-mailové zasielanie objednávok
rozšírená kontrola kategórií zbraní podľa CZ legislatívy 
