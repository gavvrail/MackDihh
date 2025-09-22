# 🚀 Project Setup Guide

This guide will walk you through the necessary steps to set up the project database in Visual Studio 2022.

---

## 📋 Prerequisites

First things first, you'll need the database file.

* **Download the Database:** [Click here to download the database from Google Drive](https://drive.google.com/drive/u/0/folders/11uMzZKU8AYpJHXJImZKcoKRTZLHhAxjT)

---

## ⚙️ Step 1: Connect the Database

Follow these steps to connect the downloaded database file to your project.

1.  In Visual Studio 2022, navigate to **View > Server Explorer**.
2.  Right-click on **Data Connections** and select **Add Connection**.
3.  In the 'Add Connection' window, click the **Change...** button.
4.  Select **Microsoft SQL Server Database File** and click **OK**.
5.  For the database file name, browse and select the `.mdf` file you just downloaded.

---

## 🔗 Step 2: Get the Database Path

Now, you'll need to get the file path for the newly connected database.

1.  Go back to the **Server Explorer** (**View > Server Explorer**).
2.  Under **Data Connections**, find the database you just added (it will have an `.mdf` extension).
3.  Right-click on the database and select **Properties**.
4.  In the Properties window, find the **Connection String** and copy the full path to the `.mdf` file.

---

## 📝 Step 3: Update `appsettings.json`

The next step is to update the connection string in your project's configuration.

1.  Open the **Solution Explorer**.
2.  Find and open the `appsettings.json` file.
3.  Locate the `"ConnectionStrings"` section and replace it with the following:

    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\TheirName\\PathTo\\YourDatabase.mdf;Integrated Security=True;"
    }
    ```

4.  **Crucially**, replace `"C:\\Users\\TheirName\\PathTo\\YourDatabase.mdf"` with the path you copied in the previous step.

    > **⚠️ Important:** Ensure your path uses double backslashes (`\\`) and not single (`\`) or quadruple (`\\\\`) backslashes.
    >
    > * **Correct:** `Data Source=(LocalDB)\\MSSQLLocalDB` ✅
    > * **Incorrect:** `Data Source=(LocalDB)\\\\MSSQLLocalDB` ❌

---

## 🎉 Final Step: Update the Database

You're almost there! The last step is to run the database update command.

1.  Go to **Tools > NuGet Package Manager > Package Manager Console**.
2.  In the console window that appears, type the following command and press Enter:

    ```powershell
    Update-Database
    ```
---

## 🔑 Default Login Credentials

Once the application is running, you can use these default accounts to log in and test the application.

* **Admin Account**
    * **Username:** `admin`
    * **Password:** `Password123!`

* **Customer Account**
    * **Username:** `polarbear`
    * **Password:** `Polar123!`

And that's it! Your project should now be correctly configured to use the database.
