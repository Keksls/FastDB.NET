# FastDB.NET

FastDB.NET is an embedded, file-backed data store for .NET game data.

It is designed for fast in-memory reads, compact binary persistence, simple editing from external tools, and predictable save/load behavior for game servers, tools, and content pipelines.

## Highlights

- Typed tables: string, int, uint, float, double, bool, Date/DateTime, byte[].
- Fast in-memory rows with optional hash indexes and unique indexes.
- Fluent Select / Update / Delete API.
- Atomic binary save: writes to a temp file, flushes, then replaces the target.
- Versioned binary format with validation on counts and payload sizes.
- Optional AES-GCM encryption via `Lock(password)`.
- Batch mode for high-volume imports without autosave noise.
- JSON export/import for tooling and external editors.
- CSV export/import per table.
- WinForms browser project retargeted to modern .NET.

## Quick Start

```csharp
using FastDB.NET;

var db = new FastDatabase("GameData", "Data");

Table items = db.CreateTable("Items");
items.AddField("Id", FastDBType.Integer);
items.AddField("Code", FastDBType.String);
items.AddField("Level", FastDBType.Integer);
items.AddField("Blob", FastDBType.ByteArray);

items.CreateIndex("Id", unique: true);
items.CreateIndex("Code");

db.Batch(_ =>
{
    items.Insert(1, "sword_001", 5, new byte[] { 1, 2, 3 });
    items.Insert(2, "potion_001", 1, new byte[] { 4, 5 });
});

db.Save();
```

## Load

```csharp
var db = new FastDatabase("GameData", "Data").Connect();
Table items = db.GetTable("Items");
```

## Query

```csharp
Row sword = items
    .Select("Code", "Level")
    .Where("Id", DBCondition.Equal, 1)
    .FirstOrDefault();

List<string> starterItems = items
    .Select("Code")
    .Where("Level", DBCondition.LessOrEqual, 5)
    .Execute<string>();
```

Equality conditions automatically use an index when one exists.

## Update / Delete

```csharp
int updated = items
    .Update()
    .Where("Code", DBCondition.Equal, "potion_001")
    .Set("Level", 3)
    .Execute();

int removed = items
    .Delete()
    .Where("Level", DBCondition.LessThan, 1)
    .Execute();
```

## External Editing

Use JSON when you want a whole database snapshot that tools can read and write:

```csharp
db.ExportJson("GameData.json");
FastDatabase imported = FastDatabase.ImportJson("GameData.json", "GameDataEdited", "Data");
imported.Save();
```

Use CSV for spreadsheet-style edits on a single table:

```csharp
items.ExportCsv("Items.csv");
items.ImportCsv("Items.csv", clearExisting: true);
db.Save();
```

Byte arrays are exported as Base64 strings.

## Encryption

```csharp
db.Lock("strong-password");
db.Save();

var lockedDb = new FastDatabase("GameData", "Data").Connect("strong-password");
lockedDb.UnLock("strong-password");
lockedDb.Save();
```

Encryption is optional. Keep it off for maximum save/load throughput when the file does not need confidentiality.

## Performance Smoke Test

Run:

```powershell
dotnet run --project FastDB.NET_Test\FastDB.NET_Test.csproj -c Release -v:minimal -- --perf
```

Current local smoke result on this workspace:

- Insert 1,000,000 rows: ~1.1 s
- Indexed lookup: ~160 ticks
- Save 1,000,000 rows: ~177 ms
- Load 1,000,000 rows: ~926 ms
- File size: ~35 MB

These numbers are a smoke test, not a formal benchmark. Real game schemas should be benchmarked with representative row counts, strings, blobs, and access patterns.

## Complex MMORPG Smoke Test

Run:

```powershell
dotnet run --project FastDB.NET_Test\FastDB.NET_Test.csproj -c Release -v:minimal -- --complex
```

It creates `fastdb-smoke\ComplexGameData.FastDB` for opening in the browser. The dataset includes:

- `DataTypes_All`: one row covering every FastDB type.
- `Relations`: human-readable relation metadata between tables.
- `Accounts`, `Guilds`, `Characters`, `ItemDefinitions`.
- `Inventory`: the large relation-heavy table.
- `QuestProgress`, `Mail`, `MarketOrders`, `WorldSpawns`.
- Hash indexes and unique indexes on the main identifiers and relation keys.

Current local smoke result:

- Total rows: 1,512,512
- Insert: ~3.5 s
- Indexed relation checks: <1 ms
- Save: ~659 ms
- Load: ~3.0 s
- File size: ~88 MB

For a larger dataset:

```powershell
dotnet run --project FastDB.NET_Test\FastDB.NET_Test.csproj -c Release -v:minimal -- --complex --complex-large
```
