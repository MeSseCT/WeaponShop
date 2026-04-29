# WeaponShop

WeaponShop je webová aplikácia pre evidenciu a predaj zbraní a doplnkov postavená na ASP.NET Core MVC a princípoch Clean Architecture.


## Použité technológie

- .NET 8 SDK (`8.0.416` podľa `global.json`)
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

Pred spustením maj nainštalované:

- .NET 8 SDK
- PostgreSQL 16

Voliteľné:

- Docker + Docker Compose, ak chceš databázu spustiť cez kontajner

NuGet balíčky sa neinštalujú ručne. Stačí spustiť `dotnet restore`.

## Rýchly štart

### 1. Obnov závislosti

```bash
dotnet restore
```

### 2. Spusti PostgreSQL

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

Vytvor databázu a používateľa podľa vlastného nastavenia a potom nastav connection string v ďalšom kroku.

### 3. Nastav lokálne secrets

Najjednoduchšia cesta je interaktívny skript:

```bash
./scripts/setup-local-secrets.sh
```

Alebo nastav hodnoty ručne:

```bash
cd WeaponShop.web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=weaponshop_dev_password;Include Error Detail=true"
dotnet user-secrets set "SeedAdmin:Password" "CHANGE_ME"
dotnet user-secrets set "SeedWarehouse:Password" "CHANGE_ME"
dotnet user-secrets set "SeedGunsmith:Password" "CHANGE_ME"
dotnet user-secrets set "Email:Smtp:User" "CHANGE_ME"
dotnet user-secrets set "Email:Smtp:Password" "CHANGE_ME"
```

Alternatívne môžeš použiť environment variables:

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
- Seed heslá sú potrebné, ak chceš pri štarte vytvoriť predvolených používateľov.
- SMTP údaje sú voliteľné. Bez nich nebude fungovať odosielanie e-mailov.

### 4. Spusti aplikáciu

```bash
dotnet run --project WeaponShop.web/WeaponShop.Web.csproj
```

Pri štarte sa aplikácia pokúsi automaticky:

- pripojiť na databázu
- aplikovať EF Core migrácie
- naplniť seed dáta

Ak databáza nie je dostupná, web sa spustí aj bez automatickej migrácie a seedu.

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
