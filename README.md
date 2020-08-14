**What is FastDB.NET**

FastDB.NET is a serverless Database system for .NET.
Very fast and easy to use, it allow you to store and edit data without any knowlege about Databases.
No SQL query, no transaction thing, only a simple C# API.
FastDB.NET allow you to perfome Insertion, Selection, Update and Delete Statements.
You can Lock a database with a password and the binary file will be Encrypted.

A Browser is also available for creating and editing your databases.

**How to use FastDB.NET**<br/>
    *1) Create a Database instance*<br/>
 `FastDatabase database = new FastDatabase("DatabaseName", "FolderPath");`<br/>
    *2) Save a Database*<br/>
 `database.Save();`<br/>
    *3) Load a saved Database*<br/>
 `FastDatabase database = new FastDatabase("DatabaseName", "FolderPath");`<br/>
 `database.Open()`<br/>