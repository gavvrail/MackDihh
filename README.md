Download the Database in https://drive.google.com/drive/u/0/folders/11uMzZKU8AYpJHXJImZKcoKRTZLHhAxjT

Go to your VS2022 and follow these steps:

**Step 1**
- View > Server Explorer
- right click the Data Connections > Add Connection
- Inside the Add connection window you click the Change button then select Microsoft SQL Server Database File then click ok
- for the database file name select the database you just downloaded

**Step 2**
- After adding the database, u go to View > Server Explorer 
- Find a section called Data Connections
- find the .mdf database and right click it and select Properties
- In the properties there copy the path of .mdf file

**Step 3**
- After complete this u need to update your appsettings.json also (Can be found in Solution Explorer)
- open appsettings.json and find the "ConnectionStrings" section and replace it with this:

"ConnectionStrings": {
  "DefaultConnection": "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\TheirName\\PathTo\\YourDatabase.mdf;Integrated Security=True;"
}

- Make sure to change the "C:\\Users\\TheirName\\PathTo\\YourDatabase.mdf" to the path that u copied earlier

**Last step**
- Go to Tools > NuGet Package Manager > Package Manager Console
- Type the command: Update-Database
