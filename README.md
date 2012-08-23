UniDyno
===

ORM for ADO.NET 
You can read article from http://www.kenanhancer.com/uni-orm-for-ado-net/ 

How To Install It?
------------------
Drop Uni C#.NET code file into your project and change it as you wish.

How Do You Use It?
------------------
Let's say that you installed database connectors in your machine.
Your project doesn't need any DLL in references. Uni will find DLL which is necessary from the GAC.

Let's have a look at config file for connectionStrings which will be used by Uni.
```csharp
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="AdventureWorks" connectionString="Data Source=localhost;Initial Catalog=AdventureWorks2012;Integrated Security=True" providerName="System.Data.SqlClient"/>
    <add name="HR" connectionString="DATA SOURCE=localhost;PASSWORD=1;PERSIST SECURITY INFO=True;USER ID=HR" providerName="Oracle.DataAccess.Client"/>
    <add name="Sakila" connectionString="server=localhost;Uid=root;Pwd=1;database=sakila;Allow User Variables=true;" providerName="MySql.Data.MySqlClient"/>
  </connectionStrings>
</configuration>
```
Uni can understand database that you want to use with providerName attribute in connectionString.
Let's say we want to use Oracle database. We should set providerName as "Oracle.DataAccess.Client" as shown in config code.

We can create Uni object now.
```csharp
var aw = new Uni("AdventureWorks");//Microsoft SQL Server
var hr = new Uni("HR");//Oracle
var sakila = new Uni("Sakila");//MySQL
```

If you don't want to use config file, you can create Uni object with connectionString directly.
```csharp
var aw = new Uni(@"Data Source=localhost;Initial Catalog=AdventureWorks2012;Integrated Security=True", DatabaseType.SQLServer);//Microsoft SQL Server
var hr = new Uni(@"DATA SOURCE=localhost;PASSWORD=1;PERSIST SECURITY INFO=True;USER ID=HR", DatabaseType.Oracle);//Oracle
var sakila = new Uni(@"server=localhost;Uid=root;Pwd=1;database=sakila;Allow User Variables=true;", DatabaseType.MySQL);//MySQL
```

How To Execute a Query?
-----------------------
Let's say we want to Query "Product" table. So, You just need to instantiate it inline.
```csharp
//returns all the products
var result = aw.dyno.Query(Schema: "Production", Table: "Product");
```
Actually, after you write "aw.dyno" and click the point button, you will not see intellisense. Because, Methods and arguments after "aw.dyno" code
are on the fly. But, Uni is smart and dynamic. So, it will generate and execute query according to your method and parameters.

```csharp
//if you want to use dynamic advantages, you should use dynamic. 
//But, if you use like that, you will lose intellisense. So, you will not use other static methods of Uni.
dynamic aw = new Uni("AdventureWorks");
var result = aw.Query(Schema: "Production", Table: "Product");

//if you use as bellow, you just use dyno property without extra code
var aw = new Uni("AdventureWorks");
var result = aw.dyno.Query(Schema: "Production", Table: "Product");
//you can use intellisense here
var result = aw.Count(commandType: System.Data.CommandType.TableDirect, schema: "Production", commandText: "Product");
```

You can also run classic queries.
```csharp
var result = aw.dyno.Query(Sql: "SELECT * FROM [Production].[Product]");
```

Dynamic object and strongly typed result
----------------------------------------
Let's say you want to use POCO model, you can set your POCO type as generic in method. So, Uni will return strongly typed result.
```csharp
public class customer
{
    public int customer_id { get; set; }
    public int store_id { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public int address_id { get; set; }
    public bool active { get; set; }
    public DateTime create_date { get; set; }
    public DateTime last_update { get; set; }
}

//Execute and return strongly typed result
var result = sakila.dyno.Query<customer>(Table: "customer");

//Execute and return dynamic object result
var result = sakila.dyno.Query(Table: "customer");
```

Some example codes
------------------
```csharp
//Actually, you can use Uni in several ways. below four lines of code will return same result.
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: "Adjustable Race");

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: new object[] { "Adjustable Race" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Args: new { Name = "Adjustable Race" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Name: "Adjustable Race");

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Name: "Adjustable Race");


//below three lines of code will return same result.
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@1 and ListPrice=@0", Args: new object[] { 0, "Black" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color and ListPrice=@ListPrice", Args: new { ListPrice = 0, Color = "Black" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color and ListPrice=@ListPrice", ListPrice: 0, Color: "Black");

var awResult10 = aw.dyno.Query(Schema: "Production", Table: "Product", ListPrice: 0, Color: "Black");

//After this method runs, generated query will be below line. So, Uni have some standart arguments. But, others will be criteria.
//Let's look at below SQL query "Color" and "ListPrice" arguments added as criteria.
//SELECT ProductID,Name,ProductNumber FROM [Production].[Product] WHERE ListPrice=@ListPrice AND Color=@Color ORDER BY ProductID DESC
var awResult11 = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", OrderBy: "ProductID DESC", ListPrice: 0, Color: "Black");
```