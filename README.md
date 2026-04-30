# WeaponShop

WeaponShop je webová aplikácia pre evidenciu a predaj zbraní a doplnkov postavená na ASP.NET Core MVC a princípoch Clean Architecture.


## Použité technológie

- .NET 8 SDK 
- ASP.NET Core MVC
- Entity Framework Core 8
- PostgreSQL 16
- ASP.NET Core Identity
- MailKit
- QuestPDF
- xUnit

## Štruktúra riešenia

- `WeaponShop.Domain` - doménové entity a enumy
- `WeaponShop.Application` - aplikačná logika, pravidlá a služby
- `WeaponShop.Infrastructure` - databáza, EF Core, repozitáre, e-mail, faktúry
- `WeaponShop.web` - MVC webová vrstva
- `WeaponShop.Tests` - automatické testy

## Požiadavky

Pred spustením projektu je potrebné mať nainštalované:

- .NET 8 SDK
- PostgreSQL 16

Voliteľné:

- Docker + Docker Compose, ak má byť databáza spustená cez kontajner

NuGet balíčky sa neinštalujú ručne. Na ich obnovenie slúži príkaz `dotnet restore`.

## Rýchly štart

### 1. Obnovenie závislostí

```bash
dotnet restore
```

### 2. Spustenie PostgreSQL

Možnosť A: cez Docker Compose

```bash
docker compose up -d
```

Predvolená databáza z `docker-compose.yml`:

- host: `localhost`
- port: `5432`
- database: `weaponshop`
- user: `weaponshop`
- password: `weaponshop_dev_password`

Možnosť B: vlastná lokálna PostgreSQL inštancia

V tomto prípade je potrebné vytvoriť databázu a používateľa podľa zvoleného nastavenia a následne doplniť connection string v ďalšom kroku.

### 3. Nastavenie lokálnych secrets

Najjednoduchší postup predstavuje interaktívny skript:

```bash
./scripts/setup-local-secrets.sh
```

Alternatívou je ručné nastavenie hodnôt:

```bash
cd WeaponShop.web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=weaponshop_dev_password;Include Error Detail=true"
dotnet user-secrets set "SeedAdmin:Password" "CHANGE_ME"
dotnet user-secrets set "SeedWarehouse:Password" "CHANGE_ME"
dotnet user-secrets set "SeedGunsmith:Password" "CHANGE_ME"
dotnet user-secrets set "Email:Smtp:User" "CHANGE_ME"
dotnet user-secrets set "Email:Smtp:Password" "CHANGE_ME"
```

Alternatívne je možné použiť environment variables:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=weaponshop_dev_password;Include Error Detail=true"
export SeedAdmin__Password="CHANGE_ME"
export SeedWarehouse__Password="CHANGE_ME"
export SeedGunsmith__Password="CHANGE_ME"
export Email__Smtp__User="CHANGE_ME"
export Email__Smtp__Password="CHANGE_ME"
```

Poznámky:

- `ConnectionStrings:DefaultConnection` je povinný.
- Seed heslá sú potrebné, ak sa majú pri štarte vytvoriť predvolení používatelia.
- SMTP údaje sú voliteľné. Bez nich nebude fungovať odosielanie e-mailov.
- Ak sa pri lokálnom vývoji nemajú používať e-mailové notifikácie, odporúča sa ponechať `Email:Smtp:Host` nenastavený. V takom prípade sa odoslanie e-mailu bezpečne preskočí.
- Ak je SMTP host nastavený, ale server je nedostupný alebo reaguje pomaly, databázová zmena sa uloží, ale odpoveď requestu sa môže oneskoriť kvôli pokusu o odoslanie e-mailu. Po obnovení stránky už budú zmeny v košíku, objednávke alebo stave objednávky viditeľné, aj keď sa e-mail nakoniec neodošle.

### 4. Spustenie aplikácie

```bash
dotnet run --project WeaponShop.web/WeaponShop.Web.csproj
```

Pri štarte sa aplikácia pokúsi automaticky:

- pripojiť na databázu
- aplikovať EF Core migrácie
- naplniť seed dáta

Ak databáza nie je dostupná, webová aplikácia sa spustí aj bez automatickej migrácie a seedu.

## Testy

Spustenie testov:

```bash
dotnet test
```

## Užitočné príkazy

Obnovenie balíčkov a build:

```bash
dotnet restore
dotnet build
```

Zastavenie databázy cez Docker Compose:

```bash
docker compose down
```

## Konfigurácia

V repozitári sú ne-citlivé defaultné nastavenia pre:

- seed účty (`admin@weaponshop.local`, `warehouse@weaponshop.local`, `gunsmith@weaponshop.local`)
- SMTP host a port
- fakturačné údaje pre generovanie dokladov

Citlivé údaje ako heslá a connection stringy nepatria do repozitára a nastavujú sa cez `user-secrets` alebo environment variables.
