using FastDB.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FastDB.NET_Test
{
    internal static class Program
    {
        private static readonly string WorkDir = Path.Combine(Environment.CurrentDirectory, "fastdb-smoke");

        private static void Main(string[] args)
        {
            Directory.CreateDirectory(WorkDir);
            RunCorrectnessChecks();
            RunPerformanceSmoke(args.Contains("--perf"));

            if (args.Contains("--complex"))
                RunComplexitySmoke(args.Contains("--complex-large"));
        }

        private static void RunCorrectnessChecks()
        {
            string dbPath = Path.Combine(WorkDir, "GameData.FastDB");
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            FastDatabase db = new FastDatabase("GameData", WorkDir);
            Table items = db.CreateTable("Items");
            items.AddField("Id", FastDBType.Integer);
            items.AddField("Code", FastDBType.String);
            items.AddField("Level", FastDBType.Integer);
            items.AddField("Blob", FastDBType.ByteArray);
            items.CreateIndex("Id", unique: true);
            items.CreateIndex("Code");

            Assert(items.Insert(1, "sword_001", 5, new byte[] { 1, 2, 3 }), "insert 1");
            Assert(items.Insert(2, "potion_001", 1, new byte[] { 4, 5 }), "insert 2");

            Row selected = items.Select("Code", "Level").Where("Id", DBCondition.Equal, 1).FirstOrDefault();
            Assert(selected != null, "select by indexed id");
            Assert(selected.Get<string>("Code") == "sword_001", "projected row string getter");
            Assert(selected.Get<int>("Level") == 5, "projected row int getter");

            int updated = items.Update().Where("Code", DBCondition.Equal, "potion_001").Set("Level", 3).Execute();
            Assert(updated == 1, "update count");
            Assert(items.Select("Level").Where("Id", DBCondition.Equal, 2).Execute<int>()[0] == 3, "updated value");

            items.RemoveField("Blob");
            Assert(items.NbFields == 3, "remove non-terminal field");

            db.Save();
            FastDatabase loaded = new FastDatabase("GameData", WorkDir).Connect();
            Assert(loaded.GetTable("Items").NbRows == 2, "roundtrip row count");
            Assert(loaded.GetTable("Items").TryGetByIndex("Id", 1, out Row row) && row.Get<string>("Code") == "sword_001", "index roundtrip");

            string jsonPath = Path.Combine(WorkDir, "GameData.json");
            loaded.ExportJson(jsonPath);
            FastDatabase imported = FastDatabase.ImportJson(jsonPath, "ImportedGameData", WorkDir);
            Assert(imported.GetTable("Items").NbRows == 2, "json import");

            string lockedPath = Path.Combine(WorkDir, "LockedGameData.FastDB");
            if (File.Exists(lockedPath))
                File.Delete(lockedPath);
            loaded.SaveAs("LockedGameData", WorkDir, "secret");
            FastDatabase unlocked = new FastDatabase("LockedGameData", WorkDir).Connect("secret");
            Assert(unlocked.GetTable("Items").NbRows == 2, "encrypted roundtrip");

            Console.WriteLine("Correctness checks: OK");
        }

        private static void RunPerformanceSmoke(bool large)
        {
            int count = large ? 1000000 : 100000;
            FastDatabase db = new FastDatabase("Perf", WorkDir);
            Table players = db.CreateTable("Players");
            players.AddField("Id", FastDBType.Integer);
            players.AddField("Name", FastDBType.String);
            players.AddField("Level", FastDBType.Integer);
            players.AddField("Gold", FastDBType.Double);
            players.CreateIndex("Id", unique: true);

            Stopwatch sw = Stopwatch.StartNew();
            db.Batch(database =>
            {
                for (int i = 0; i < count; i++)
                    players.Insert(i, "Player_" + i, i % 100, i * 2.5d);
            }, true);
            sw.Stop();
            Console.WriteLine("Insert " + count.ToString("N0") + " rows: " + sw.ElapsedMilliseconds + " ms");

            sw.Restart();
            bool found = players.TryGetByIndex("Id", count - 1, out Row last);
            sw.Stop();
            Assert(found && last.Get<string>("Name") == "Player_" + (count - 1), "indexed lookup");
            Console.WriteLine("Indexed lookup: " + sw.ElapsedTicks + " ticks");

            sw.Restart();
            db.Save();
            sw.Stop();
            FileInfo file = new FileInfo(Path.Combine(WorkDir, "Perf.FastDB"));
            Console.WriteLine("Save: " + sw.ElapsedMilliseconds + " ms, " + file.Length.ToPrettySize());

            sw.Restart();
            FastDatabase loaded = new FastDatabase("Perf", WorkDir).Connect();
            sw.Stop();
            Assert(loaded.GetTable("Players").NbRows == count, "load row count");
            Console.WriteLine("Load: " + sw.ElapsedMilliseconds + " ms");
        }

        private static void RunComplexitySmoke(bool large)
        {
            string dbName = large ? "ComplexGameDataLarge" : "ComplexGameData";
            string dbPath = Path.Combine(WorkDir, dbName + ".FastDB");
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            int accountCount = large ? 25000 : 10000;
            int guildCount = large ? 1200 : 500;
            int characterCount = large ? 125000 : 50000;
            int itemDefCount = large ? 5000 : 2000;
            int inventorySlotsPerCharacter = large ? 28 : 20;
            int questRowsPerCharacter = large ? 8 : 5;
            int mailCount = large ? 150000 : 50000;
            int marketOrderCount = large ? 150000 : 50000;
            int spawnCount = large ? 250000 : 100000;

            FastDatabase db = new FastDatabase(dbName, WorkDir);
            Stopwatch total = Stopwatch.StartNew();
            DateTime baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            Table dataTypes = CreateDataTypeCoverageTable(db);
            Table relations = CreateRelationsTable(db);
            Table accounts = CreateAccountsTable(db);
            Table guilds = CreateGuildsTable(db);
            Table characters = CreateCharactersTable(db);
            Table itemDefinitions = CreateItemDefinitionsTable(db);
            Table inventory = CreateInventoryTable(db);
            Table questProgress = CreateQuestProgressTable(db);
            Table mails = CreateMailTable(db);
            Table marketOrders = CreateMarketOrdersTable(db);
            Table worldSpawns = CreateWorldSpawnsTable(db);

            InsertRelations(relations);

            Stopwatch sw = Stopwatch.StartNew();
            db.Batch(database =>
            {
                dataTypes.Insert(
                    1,
                    "all_types_reference",
                    42,
                    4200000000u,
                    123.45f,
                    987654321.123d,
                    true,
                    baseTime.Date,
                    baseTime,
                    MakeBytes(16, 99));

                for (int i = 1; i <= accountCount; i++)
                {
                    accounts.Insert(
                        i,
                        "account_" + i,
                        Region(i),
                        baseTime.AddDays(-(i % 2200)),
                        baseTime.AddMinutes(-(i * 7) % 900000),
                        i % 997 == 0,
                        baseTime.Date.AddDays(i % 365),
                        MakeBytes(12, i),
                        Math.Round((i % 50000) * 0.125d, 3),
                        (uint)(i % 1024));
                }

                for (int i = 1; i <= guildCount; i++)
                {
                    guilds.Insert(
                        i,
                        "Guild_" + i,
                        baseTime.Date.AddDays(-(i % 1500)),
                        ((i * 97) % characterCount) + 1,
                        (uint)((i % 50) + 1),
                        Math.Round(i * 1234.567d, 2),
                        MakeBytes(32, i * 3));
                }

                for (int i = 1; i <= characterCount; i++)
                {
                    int accountId = ((i - 1) % accountCount) + 1;
                    int guildId = i % 7 == 0 ? 0 : ((i - 1) % guildCount) + 1;
                    characters.Insert(
                        i,
                        accountId,
                        guildId,
                        "Char_" + i,
                        (i % 12) + 1,
                        (i % 80) + 1,
                        Coordinate(i, 31),
                        Coordinate(i, 17),
                        Coordinate(i, 7),
                        Math.Round((i * 0.777d) % 360d, 4),
                        i % 19 == 0,
                        baseTime.AddDays(-(i % 1500)),
                        baseTime.AddSeconds(-(i * 13) % 2000000),
                        baseTime.Date.AddDays(-(i % 3650)),
                        (uint)(i % 4096),
                        MakeBytes(24, i));
                }

                for (int i = 1; i <= itemDefCount; i++)
                {
                    itemDefinitions.Insert(
                        i,
                        "item_" + i.ToString("D5"),
                        ItemKind(i),
                        (i % 99) + 1,
                        (float)Math.Round(0.05d + (i % 400) * 0.015d, 3),
                        Math.Round(1.0d + i * 2.75d, 2),
                        i % 11 != 0,
                        baseTime.Date.AddDays(-(i % 900)),
                        MakeBytes(20, i * 5));
                }

                int inventoryId = 1;
                for (int characterId = 1; characterId <= characterCount; characterId++)
                {
                    for (int slot = 0; slot < inventorySlotsPerCharacter; slot++)
                    {
                        int itemDefId = ((characterId * 31 + slot * 17) % itemDefCount) + 1;
                        inventory.Insert(
                            inventoryId++,
                            characterId,
                            slot,
                            itemDefId,
                            (uint)((slot % 6) + 1),
                            (float)Math.Round(100f - ((characterId + slot) % 100) * 0.37f, 3),
                            slot % 13 == 0,
                            baseTime.AddMinutes(-(characterId * 3 + slot) % 1000000),
                            MakeBytes(8, characterId + slot));
                    }
                }

                int questRowId = 1;
                for (int characterId = 1; characterId <= characterCount; characterId++)
                {
                    for (int questOffset = 0; questOffset < questRowsPerCharacter; questOffset++)
                    {
                        int questId = ((characterId * 13 + questOffset) % 4000) + 1;
                        int state = (characterId + questOffset) % 4;
                        questProgress.Insert(
                            questRowId++,
                            characterId,
                            questId,
                            state,
                            (uint)((characterId + questOffset * 3) % 100),
                            baseTime.AddHours(-(characterId + questOffset) % 50000),
                            state == 3 ? baseTime.AddHours(-(characterId + questOffset) % 20000) : default(DateTime),
                            "quest_state_" + state);
                    }
                }

                for (int i = 1; i <= mailCount; i++)
                {
                    int sender = ((i * 23) % characterCount) + 1;
                    int recipient = ((i * 29) % characterCount) + 1;
                    mails.Insert(
                        i,
                        sender,
                        recipient,
                        "Mail subject " + i,
                        "Generated body for complexity smoke " + i,
                        baseTime.AddMinutes(-i),
                        i % 5 == 0,
                        i % 5 == 0 ? ((i * 11) % itemDefCount) + 1 : 0,
                        MakeBytes(18, i));
                }

                for (int i = 1; i <= marketOrderCount; i++)
                {
                    marketOrders.Insert(
                        i,
                        ((i * 37) % characterCount) + 1,
                        ((i * 41) % itemDefCount) + 1,
                        (uint)((i % 50) + 1),
                        Math.Round(10.0d + (i % 25000) * 0.33d, 2),
                        baseTime.AddMinutes(-i * 2),
                        baseTime.AddDays((i % 30) + 1),
                        i % 3 == 0);
                }

                for (int i = 1; i <= spawnCount; i++)
                {
                    worldSpawns.Insert(
                        i,
                        (i % 64) + 1,
                        SpawnKind(i),
                        Coordinate(i, 11),
                        Coordinate(i, 13),
                        Coordinate(i, 5),
                        (uint)(30 + (i % 3600)),
                        (i % 2000) + 1,
                        i % 17 != 0,
                        MakeBytes(16, i * 7));
                }
            });
            sw.Stop();
            Console.WriteLine("Complex insert: " + TotalRows(db).ToString("N0") + " rows in " + sw.ElapsedMilliseconds + " ms");

            sw.Restart();
            Assert(characters.TryGetByIndex("CharacterId", characterCount, out Row lastCharacter), "complex indexed character lookup");
            Assert(lastCharacter.Get<int>("AccountId") >= 1, "complex relation field present");
            Assert(inventory.Select("ItemDefId").Where("CharacterId", DBCondition.Equal, characterCount).Count() == inventorySlotsPerCharacter, "complex inventory relation count");
            sw.Stop();
            Console.WriteLine("Complex indexed relation checks: " + sw.ElapsedMilliseconds + " ms");

            sw.Restart();
            db.Save();
            sw.Stop();
            FileInfo file = new FileInfo(dbPath);
            Console.WriteLine("Complex save: " + sw.ElapsedMilliseconds + " ms, " + file.Length.ToPrettySize());

            sw.Restart();
            FastDatabase loaded = new FastDatabase(dbName, WorkDir).Connect();
            sw.Stop();
            Assert(TotalRows(loaded) == TotalRows(db), "complex load row count");
            Assert(loaded.GetTable("DataTypes_All").Select("UIntValue").Where("Id", DBCondition.Equal, 1).Execute<uint>()[0] == 4200000000u, "complex uint roundtrip");
            Assert(loaded.GetTable("DataTypes_All").Select("BlobValue").Where("Id", DBCondition.Equal, 1).FirstOrDefault().GetByteArray("BlobValue").Length == 16, "complex blob roundtrip");
            Console.WriteLine("Complex load: " + sw.ElapsedMilliseconds + " ms");

            total.Stop();
            Console.WriteLine("Complex database saved for browser: " + dbPath);
            Console.WriteLine("Complex total: " + total.ElapsedMilliseconds + " ms");
        }

        private static Table CreateDataTypeCoverageTable(FastDatabase db)
        {
            Table table = db.CreateTable("DataTypes_All");
            table.AddField("Id", FastDBType.Integer);
            table.AddField("StringValue", FastDBType.String);
            table.AddField("IntValue", FastDBType.Integer);
            table.AddField("UIntValue", FastDBType.UnsignedInteger);
            table.AddField("FloatValue", FastDBType.Float);
            table.AddField("DoubleValue", FastDBType.Double);
            table.AddField("BoolValue", FastDBType.Bool);
            table.AddField("DateValue", FastDBType.Date);
            table.AddField("DateTimeValue", FastDBType.DateTime);
            table.AddField("BlobValue", FastDBType.ByteArray);
            table.CreateIndex("Id", unique: true);
            return table;
        }

        private static Table CreateRelationsTable(FastDatabase db)
        {
            Table table = db.CreateTable("Relations");
            table.AddField("RelationId", FastDBType.Integer);
            table.AddField("SourceTable", FastDBType.String);
            table.AddField("SourceField", FastDBType.String);
            table.AddField("TargetTable", FastDBType.String);
            table.AddField("TargetField", FastDBType.String);
            table.AddField("Cardinality", FastDBType.String);
            table.AddField("Description", FastDBType.String);
            table.CreateIndex("RelationId", unique: true);
            table.CreateIndex("SourceTable");
            table.CreateIndex("TargetTable");
            return table;
        }

        private static Table CreateAccountsTable(FastDatabase db)
        {
            Table table = db.CreateTable("Accounts");
            table.AddField("AccountId", FastDBType.Integer);
            table.AddField("UserName", FastDBType.String);
            table.AddField("Region", FastDBType.String);
            table.AddField("CreatedAt", FastDBType.DateTime);
            table.AddField("LastLoginAt", FastDBType.DateTime);
            table.AddField("IsBanned", FastDBType.Bool);
            table.AddField("PremiumUntil", FastDBType.Date);
            table.AddField("SecuritySalt", FastDBType.ByteArray);
            table.AddField("CashBalance", FastDBType.Double);
            table.AddField("Flags", FastDBType.UnsignedInteger);
            table.CreateIndex("AccountId", unique: true);
            table.CreateIndex("UserName", unique: true);
            table.CreateIndex("Region");
            return table;
        }

        private static Table CreateGuildsTable(FastDatabase db)
        {
            Table table = db.CreateTable("Guilds");
            table.AddField("GuildId", FastDBType.Integer);
            table.AddField("Name", FastDBType.String);
            table.AddField("CreatedDate", FastDBType.Date);
            table.AddField("LeaderCharacterId", FastDBType.Integer);
            table.AddField("Level", FastDBType.UnsignedInteger);
            table.AddField("TreasuryGold", FastDBType.Double);
            table.AddField("Emblem", FastDBType.ByteArray);
            table.CreateIndex("GuildId", unique: true);
            table.CreateIndex("Name", unique: true);
            table.CreateIndex("LeaderCharacterId");
            return table;
        }

        private static Table CreateCharactersTable(FastDatabase db)
        {
            Table table = db.CreateTable("Characters");
            table.AddField("CharacterId", FastDBType.Integer);
            table.AddField("AccountId", FastDBType.Integer);
            table.AddField("GuildId", FastDBType.Integer);
            table.AddField("Name", FastDBType.String);
            table.AddField("ClassId", FastDBType.Integer);
            table.AddField("Level", FastDBType.Integer);
            table.AddField("X", FastDBType.Float);
            table.AddField("Y", FastDBType.Float);
            table.AddField("Z", FastDBType.Float);
            table.AddField("Rotation", FastDBType.Double);
            table.AddField("IsOnline", FastDBType.Bool);
            table.AddField("CreatedAt", FastDBType.DateTime);
            table.AddField("LastSaveAt", FastDBType.DateTime);
            table.AddField("Birthday", FastDBType.Date);
            table.AddField("Flags", FastDBType.UnsignedInteger);
            table.AddField("AppearanceBlob", FastDBType.ByteArray);
            table.CreateIndex("CharacterId", unique: true);
            table.CreateIndex("AccountId");
            table.CreateIndex("GuildId");
            table.CreateIndex("Name", unique: true);
            return table;
        }

        private static Table CreateItemDefinitionsTable(FastDatabase db)
        {
            Table table = db.CreateTable("ItemDefinitions");
            table.AddField("ItemDefId", FastDBType.Integer);
            table.AddField("Code", FastDBType.String);
            table.AddField("Kind", FastDBType.String);
            table.AddField("MaxStack", FastDBType.Integer);
            table.AddField("Weight", FastDBType.Float);
            table.AddField("VendorPrice", FastDBType.Double);
            table.AddField("IsTradable", FastDBType.Bool);
            table.AddField("PatchDate", FastDBType.Date);
            table.AddField("TagBlob", FastDBType.ByteArray);
            table.CreateIndex("ItemDefId", unique: true);
            table.CreateIndex("Code", unique: true);
            table.CreateIndex("Kind");
            return table;
        }

        private static Table CreateInventoryTable(FastDatabase db)
        {
            Table table = db.CreateTable("Inventory");
            table.AddField("InventoryRowId", FastDBType.Integer);
            table.AddField("CharacterId", FastDBType.Integer);
            table.AddField("Slot", FastDBType.Integer);
            table.AddField("ItemDefId", FastDBType.Integer);
            table.AddField("Quantity", FastDBType.UnsignedInteger);
            table.AddField("Durability", FastDBType.Float);
            table.AddField("Bound", FastDBType.Bool);
            table.AddField("CreatedAt", FastDBType.DateTime);
            table.AddField("RandomSeedBlob", FastDBType.ByteArray);
            table.CreateIndex("InventoryRowId", unique: true);
            table.CreateIndex("CharacterId");
            table.CreateIndex("ItemDefId");
            return table;
        }

        private static Table CreateQuestProgressTable(FastDatabase db)
        {
            Table table = db.CreateTable("QuestProgress");
            table.AddField("QuestProgressId", FastDBType.Integer);
            table.AddField("CharacterId", FastDBType.Integer);
            table.AddField("QuestId", FastDBType.Integer);
            table.AddField("State", FastDBType.Integer);
            table.AddField("Progress", FastDBType.UnsignedInteger);
            table.AddField("StartedAt", FastDBType.DateTime);
            table.AddField("CompletedAt", FastDBType.DateTime);
            table.AddField("StateText", FastDBType.String);
            table.CreateIndex("QuestProgressId", unique: true);
            table.CreateIndex("CharacterId");
            table.CreateIndex("QuestId");
            return table;
        }

        private static Table CreateMailTable(FastDatabase db)
        {
            Table table = db.CreateTable("Mail");
            table.AddField("MailId", FastDBType.Integer);
            table.AddField("SenderCharacterId", FastDBType.Integer);
            table.AddField("RecipientCharacterId", FastDBType.Integer);
            table.AddField("Subject", FastDBType.String);
            table.AddField("Body", FastDBType.String);
            table.AddField("SentAt", FastDBType.DateTime);
            table.AddField("HasAttachment", FastDBType.Bool);
            table.AddField("AttachmentItemDefId", FastDBType.Integer);
            table.AddField("PayloadBlob", FastDBType.ByteArray);
            table.CreateIndex("MailId", unique: true);
            table.CreateIndex("RecipientCharacterId");
            table.CreateIndex("SenderCharacterId");
            return table;
        }

        private static Table CreateMarketOrdersTable(FastDatabase db)
        {
            Table table = db.CreateTable("MarketOrders");
            table.AddField("OrderId", FastDBType.Integer);
            table.AddField("SellerCharacterId", FastDBType.Integer);
            table.AddField("ItemDefId", FastDBType.Integer);
            table.AddField("Quantity", FastDBType.UnsignedInteger);
            table.AddField("UnitPrice", FastDBType.Double);
            table.AddField("ListedAt", FastDBType.DateTime);
            table.AddField("ExpiresAt", FastDBType.DateTime);
            table.AddField("IsBuyOrder", FastDBType.Bool);
            table.CreateIndex("OrderId", unique: true);
            table.CreateIndex("SellerCharacterId");
            table.CreateIndex("ItemDefId");
            return table;
        }

        private static Table CreateWorldSpawnsTable(FastDatabase db)
        {
            Table table = db.CreateTable("WorldSpawns");
            table.AddField("SpawnId", FastDBType.Integer);
            table.AddField("MapId", FastDBType.Integer);
            table.AddField("EntityType", FastDBType.String);
            table.AddField("X", FastDBType.Float);
            table.AddField("Y", FastDBType.Float);
            table.AddField("Z", FastDBType.Float);
            table.AddField("RespawnSeconds", FastDBType.UnsignedInteger);
            table.AddField("LootTableId", FastDBType.Integer);
            table.AddField("Enabled", FastDBType.Bool);
            table.AddField("ScriptHashBlob", FastDBType.ByteArray);
            table.CreateIndex("SpawnId", unique: true);
            table.CreateIndex("MapId");
            table.CreateIndex("EntityType");
            return table;
        }

        private static void InsertRelations(Table relations)
        {
            int id = 1;
            AddRelation(relations, ref id, "Characters", "AccountId", "Accounts", "AccountId", "many-to-one", "A character belongs to one account.");
            AddRelation(relations, ref id, "Characters", "GuildId", "Guilds", "GuildId", "many-to-one optional", "GuildId 0 means no guild in this smoke dataset.");
            AddRelation(relations, ref id, "Guilds", "LeaderCharacterId", "Characters", "CharacterId", "one-to-one-ish", "Leader points to a character.");
            AddRelation(relations, ref id, "Inventory", "CharacterId", "Characters", "CharacterId", "many-to-one", "Inventory rows owned by a character.");
            AddRelation(relations, ref id, "Inventory", "ItemDefId", "ItemDefinitions", "ItemDefId", "many-to-one", "Inventory references item definitions.");
            AddRelation(relations, ref id, "QuestProgress", "CharacterId", "Characters", "CharacterId", "many-to-one", "Quest state per character.");
            AddRelation(relations, ref id, "Mail", "SenderCharacterId", "Characters", "CharacterId", "many-to-one", "Mail sender relation.");
            AddRelation(relations, ref id, "Mail", "RecipientCharacterId", "Characters", "CharacterId", "many-to-one", "Mail recipient relation.");
            AddRelation(relations, ref id, "Mail", "AttachmentItemDefId", "ItemDefinitions", "ItemDefId", "many-to-one optional", "0 means no attachment.");
            AddRelation(relations, ref id, "MarketOrders", "SellerCharacterId", "Characters", "CharacterId", "many-to-one", "Market seller relation.");
            AddRelation(relations, ref id, "MarketOrders", "ItemDefId", "ItemDefinitions", "ItemDefId", "many-to-one", "Market item relation.");
        }

        private static void AddRelation(Table relations, ref int id, string sourceTable, string sourceField, string targetTable, string targetField, string cardinality, string description)
        {
            relations.Insert(id++, sourceTable, sourceField, targetTable, targetField, cardinality, description);
        }

        private static long TotalRows(FastDatabase db)
        {
            long total = 0;
            foreach (Table table in db.Tables.Values)
                total += table.NbRows;
            return total;
        }

        private static string Region(int i)
        {
            string[] regions = { "EU", "NA", "ASIA", "SA", "OCE" };
            return regions[i % regions.Length];
        }

        private static string ItemKind(int i)
        {
            string[] kinds = { "Weapon", "Armor", "Consumable", "Material", "Quest", "Mount", "Gem" };
            return kinds[i % kinds.Length];
        }

        private static string SpawnKind(int i)
        {
            string[] kinds = { "Npc", "Monster", "GatherNode", "Chest", "Boss", "Vendor" };
            return kinds[i % kinds.Length];
        }

        private static float Coordinate(int i, int multiplier)
        {
            return (float)Math.Round(((i * multiplier) % 200000) / 10.0d - 10000.0d, 3);
        }

        private static byte[] MakeBytes(int length, int seed)
        {
            byte[] data = new byte[length];
            uint state = (uint)(seed * 747796405 + 2891336453);
            for (int i = 0; i < data.Length; i++)
            {
                state = state * 1664525u + 1013904223u;
                data[i] = (byte)(state >> 24);
            }
            return data;
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition)
                throw new InvalidOperationException("Check failed: " + name);
        }
    }

    public static class Ext
    {
        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;

        public static string ToPrettySize(this long value)
        {
            if (value >= OneGb)
                return Math.Round((double)value / OneGb, 2) + " GB";
            if (value >= OneMb)
                return Math.Round((double)value / OneMb, 2) + " MB";
            if (value >= OneKb)
                return Math.Round((double)value / OneKb, 2) + " KB";
            return value + " B";
        }
    }
}
