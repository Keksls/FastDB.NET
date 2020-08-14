**What is FastDB.NET**

FastDB.NET is a serverless Database system for .NET.
Very fast and easy to use, it allow you to store and edit data without any knowlege about Databases.
No SQL query, no transaction thing, only a simple C# API.
FastDB.NET allow you to perfome Insertion, Selection, Update and Delete Statements.
You can Lock a database with a password and the binary file will be Encrypted.

A Browser is also available for creating and editing your databases.

**How to use FastDB.NET**

Create a Database instance
```
 FastDatabase database = new FastDatabase("DatabaseName", "FolderPath");
```
Save a Database
```
 database.Save();
```
Load a saved Database
```
FastDatabase database = new FastDatabase("DatabaseName", "FolderPath");
 database.Open()
```
Create a Table
```
 Database.CreateTable("TBBT_Persos");
 Table mytable = Database.GetTable("TBBT_Persos");
 mytable.AddField("Name", FastDBType.String);
 mytable.AddField("Age", FastDBType.Integer);
 mytable.AddField("Size", FastDBType.Float);
```
Insert Rows
```
 mytable.Insert("Leonard", 31, 1.68f);
 mytable.Insert("Penny", 30, 1.73f);
```
Select Statement
```
mytable.Select("Age", "Size").Where("Name", DBCondition.Equal, "Penny").Execute();
```
Update Statement
```
mytableUpdate().Where("Name", DBCondition.Equal, "Penny").Set("Age", 60).Execute();
```
Delete Statement
```
mytable.Delete().Where("Name", DBCondition.Equal, "Loenard").Execute();
```
Lock Database
```
database.Lock("password");
```
Unlock Database
```
database.UnLock("password");
```
Enable or disable AutoSave
```
database.AutoSave = true;
```
note that autosave will save the database in a background thread when an edition is performed on the database. 
very efficient on small and medium database, a little more costly on huge databases.
For a better handle on your database saving, you can disable autosave and save by your own.
Auto save is serialized in the database binary file, so it will be saved too.
by default, autosave is disabled.