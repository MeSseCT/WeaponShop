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

## Lokálna konfigurácia tajomstiev

Citlivé údaje nie sú uložené v repozitári. Pred prvým spustením nastav:

```bash
cd /Users/tduraj/WeaponsShop/WeaponShop.web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=CHANGE_ME;Include Error Detail=true"
dotnet user-secrets set "SeedAdmin:Password" "CHANGE_ME"
dotnet user-secrets set "SeedWarehouse:Password" "CHANGE_ME"
dotnet user-secrets set "SeedGunsmith:Password" "CHANGE_ME"
dotnet user-secrets set "Email:Smtp:User" "CHANGE_ME"
dotnet user-secrets set "Email:Smtp:Password" "CHANGE_ME"
```

Alternatívne môžeš použiť environment variables:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=CHANGE_ME;Include Error Detail=true"
export SeedAdmin__Password="CHANGE_ME"
export SeedWarehouse__Password="CHANGE_ME"
export SeedGunsmith__Password="CHANGE_ME"
export Email__Smtp__User="CHANGE_ME"
export Email__Smtp__Password="CHANGE_ME"
```

Na rýchle lokálne nastavenie môžeš použiť aj helper skript:

```bash
/Users/tduraj/WeaponsShop/scripts/setup-local-secrets.sh
```

## Rotácia lokálneho PostgreSQL hesla

Ak používaš docker compose databázu z tohto repozitára, heslo môžeš zmeniť priamo v bežiacom kontajneri:

```bash
docker compose exec postgres psql -U weaponshop -d weaponshop -c "ALTER USER weaponshop WITH PASSWORD 'CHANGE_ME';"
```

Potom nastav rovnaké heslo aj do `ConnectionStrings:DefaultConnection` cez `user-secrets` alebo environment variables.
