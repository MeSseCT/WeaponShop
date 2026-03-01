# WeaponShop

Základ bakalárskej práce postavený na princípoch Clean Architecture.

## Použité technológie

- .NET 8
- ASP.NET Core MVC
- Entity Framework Core 8
- PostgreSQL
- ASP.NET Core Identity
- C#

## Štruktúra projektu

- `WeaponShop.Domain` – doménové entity a enumy (napr. `Weapon`, `Order`, `OrderItem`, `OrderStatus`)
- `WeaponShop.Application` – aplikačná logika a služby
- `WeaponShop.Infrastructure` – EF Core, `AppDbContext`, migrácie, repozitáre
- `WeaponShop.Web` – MVC webová vrstva (Controllers, Views)

## Aktuálny stav

- evidencia zbraní
- CRUD operácie pre zbrane
- autentifikácia/autorizácia cez ASP.NET Core Identity
- prístup podľa rolí (`Admin`, `Customer`)
- seed základných dát (role, admin používateľ, ukážkové zbrane)
- objednávkové jadro (entity `Order` a `OrderItem`)
- stavy objednávky (`OrderStatus`)
- výpočet `TotalPrice`
- databázové väzby a unikátne obmedzenie `UNIQUE(OrderId, WeaponId)`
- oddelenie vrstiev podľa Clean Architecture

## Plánované rozšírenia

- evidencia zákazníkov
- objednávky a faktúry (rozšírenie procesov)
- generovanie PDF účteniek/faktúr
- e-mailové zasielanie objednávok
- rozšírená kontrola kategórií zbraní podľa CZ legislatívy
