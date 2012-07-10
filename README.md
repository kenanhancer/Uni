UniDyno
===

ORM for ADO.NET

        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.Diagnostics;
        using System.Data;

        namespace UniORM2
        {
            public class Product
            {
                public int ProductID { get; set; }
                public string Name { get; set; }
                public string ProductNumber { get; set; }
            }
            public class COUNTRIES
            {
                public string COUNTRY_ID { get; set; }
                public string COUNTRY_NAME { get; set; }
                public decimal REGION_ID { get; set; }
            }
            public class Person
            {
                public int BusinessEntityID { get; set; }
                public string PersonType { get; set; }
                public bool NameStyle { get; set; }
                public string Title { get; set; }
                public string FirstName { get; set; }
                public string MiddleName { get; set; }
                public string LastName { get; set; }
            }
            class Program
            {
                static void Main(string[] args)
                {
                    var sakila = new Uni("Sakila");
                    var aw = new Uni("AdventureWorks");
                    var crm = new Uni("SysMgmCRM");
                    var TEST = new Uni("TEST");
                    var hr = new Uni("HR");

                    var retValue1 = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: "Adjustable Race");
                    var retValue2 = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: new object[] { "Adjustable Race" });
                    var retValue3 = aw.dyno.Query(Schema: "Production", Table: "Product", Name: "Adjustable Race");
                    var retValue4 = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Args: new { Name = "Adjustable Race" });
                    var retValue5 = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@1 and ListPrice=@0", Args: new object[] { 0, "Black" });
                    var retValue6 = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color and ListPrice=@ListPrice", ListPrice: 0, Color: "Black");
                    var retValue7 = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber");

                    var retValue8 = crm.dyno.Query(Schema: "General", Sp: "sp_TEST2", Args: new object[] { "Kenan Hançer" });
                    var retValue9 = crm.dyno.Query(Schema: "General", Sp: "sp_TEST2", Args: "Kenan Hançer");
                    var retValue10 = crm.dyno.Query(Sp: "sp_ObjectTemplateByObjID", Args: new object[] { 38, 1 });
                    var retValue11 = crm.dyno.Query(Schema: "User", Fn: "fn_GetUserByUserID", Args: new object[] { 64, 1 });

                    var retValue12 = TEST.dyno.Insert(Table: "Person", Args: new { FirstName = "kenan", LastName = "Hançer", Age = 28 });
                    var retValue13 = TEST.dyno.Insert(
                        Table: "Person",
                        Args: new object [] { 
                                      new { FirstName = "Kemal", LastName = "Hançer", Age = 56 },
                                      new { FirstName = "Kenan", LastName = "Hançer", Age = 28 },
                                      new { FirstName = "Sinan", LastName = "Hançer", Age = 27 },
                                      new { FirstName = "Kagan", LastName = "Demir" }
                        }
                    );


                    var retValue14 = aw.dyno.Exists(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });
                    var retValue15 = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });
                    var retValue16 = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice and Name=@Name", Args: new { ListPrice = 0, Name = "Adjustable Race" });
                    var retValue17 = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice and Name=@Name", ListPrice: 0, Name: "Adjustable Race");


                    //COUNT, SUM, MAX, MIN, AVG
                    var retValue18 = aw.dyno.Count(Schema: "Production", Table: "Product");
                    var retValue19 = crm.dyno.Count(Schema: "User", Fn: "fn_Users", Args: 1);
                    var retValue20 = aw.dyno.Count(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Color=@1", Args: new object[] { 0, "Black" });
                    var retValue21 = aw.dyno.Count(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@ListPrice and Color=@Color", Args: new { ListPrice = 0, Color = "Black" });
                    var retValue22 = aw.dyno.Count(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@ListPrice and Color=@Color", ListPrice: 0, Color: "Black");
                    var retValue23 = aw.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice");
                    var retValue24 = aw.dyno.Sum(Sql: "SELECT * FROM Production.Product", Columns: "ListPrice");
                    var retValue25 = aw.dyno.Sum(Sql: "SELECT * FROM Production.Product WHERE Color=@Color", Columns: "ListPrice", Args: new { Color = "Black" });

                    var retValue26 = aw.dyno.Sum(Sql: "SELECT * FROM Production.Product WHERE Color=@Color", Columns: "ListPrice", Color: "Black");
                    var retValue27 = aw.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice,SafetyStockLevel");
                    var retValue28 = aw.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice,SafetyStockLevel");
                    var retValue29 = aw.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice,SafetyStockLevel");
                    var retValue30 = aw.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice,SafetyStockLevel");
                    var retValue31 = aw.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice,SafetyStockLevel", Color: "Black");
                    var retValue32 = aw.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice,SafetyStockLevel", Color: "Red");

                    //IN Statement
                    var retValue33 = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color and Size in @Size", Args: new { Color = new[] { "Black", "Yellow", "Red" }, Size = new[] { "38", "40", "42" } });
                    var retValue34 = aw.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });
                    var retValue35 = aw.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color and Size in @Size", Color: new[] { "Black", "Yellow" }, Size: new[] { "38", "40", "42" });


                    dynamic getValue = null;
                    var retValue36 = crm.dyno.Query(CallBack: (Action<dynamic>)(f => getValue = f), Schema: "User", Table: "tbl_Users", Columns: "UserID,UserName", Where: "DepartmentID=@0 or RegionID=@1", Args: new object[] { 233, 134 });


                    var getValue37 = sakila.dyno.Query(CallBack: (Action<object>)(f => getValue = f), SP: "film_in_stock", Args: new object[] { 1, 1 });

                    var dict = ((object)getValue).ToDictionary();
                    var outputParam = dict["OutputParameters"] as IEnumerable<dynamic>;

                    var retValue38 = aw.dyno.Query(Sql: "SELECT * FROM Production.Product;SELECT * FROM Person.Person");
            
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    var retValue39 = aw.dyno.Query(Sql: "SELECT * FROM Production.Product;SELECT * FROM Person.Person");
                    sw.Stop();

                    var retValue40 = hr.dyno.Query<COUNTRIES>(Schema: "HR", Table: "COUNTRIES");
                    var retValue41 = aw.dyno.Query<Person>(Sql: "SELECT * FROM Person.Person");
                    var retValue42 = aw.dyno.Query<Person>(CallBack: (Action<object>)(f => getValue = f), Schema: "Person", Table: "Person");
                    var retValue43 = aw.dyno.Query(Schema: "Person", Sp: "GetPersonList" );
                    var retValue44 = aw.dyno.Query<Person>(Schema: "Person", Sp: "GetPersonList");


                    var retValue45 = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", OrderBy: "ProductID", Limit: 1, ListPrice: 0);
                    var retValue46 = aw.Count(commandType: System.Data.CommandType.TableDirect ,schema: "Person", commandText: "Person", callBack: (Action<object>)(f => getValue = f));
                    var retValue47 = aw.Max(commandType: System.Data.CommandType.TableDirect, schema: "Production", commandText: "Product", columns: "ListPrice", callBack: (Action<object>)(f => getValue = f));
                    var retValue48 = aw.Min(commandType: System.Data.CommandType.TableDirect, schema: "Production", commandText: "Product", columns: "ListPrice", callBack: (Action<object>)(f => getValue = f));
                    var retValue49 = aw.Avg(commandType: System.Data.CommandType.TableDirect, schema: "Production", commandText: "Product", columns: "ListPrice", callBack: (Action<object>)(f => getValue = f));
                    var retValue50 = aw.Sum(commandType: System.Data.CommandType.TableDirect, schema: "Production", commandText: "Product", columns: "ListPrice", callBack: (Action<object>)(f => getValue = f));
                    var retValue51 = aw.Sum(commandText: "SELECT * FROM Production.Product WHERE Color=@0", columns: "ListPrice", args: "Black");
            
                    //CLASSIC WAY -> COUNT, SUM, MAX, MIN, AVG
                    var retValue52 = aw.Count(commandType: CommandType.TableDirect, schema: "Production", commandText: "Product");
                    var retValue53 = aw.Count(commandType: CommandType.TableDirect, schema: "Production", commandText: "Product", where: "MakeFlag=@0", args: true);
                    var retValue54 = aw.Count(commandText: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Color=@1", args: new object[] { 0, "Black" });
                    var retValue55 = aw.Sum(commandType: CommandType.TableDirect, schema: "Production", commandText: "Product", columns: "ListPrice");
                    var retValue56 = aw.Sum(commandText: "SELECT * FROM Production.Product WHERE Color=@0", columns: "ListPrice", args: "Black");
                    var retValue57 = aw.Max(commandText: "SELECT * FROM Production.Product WHERE Color=@0", columns: "ListPrice", args: "Black");
                    var retValue58 = aw.Min(commandText: "SELECT * FROM Production.Product WHERE Color=@0", columns: "ListPrice", args: "Black");
                    var retValue59 = aw.Avg(commandText: "SELECT * FROM Production.Product WHERE Color=@0", columns: "ListPrice", args: "Black");

                    //SQL STATEMENT, EXISTS
                    var retValue60 = aw.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });
                    var retValue61 = aw.dyno.Exists(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });
                    var retValue62 = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });
                    var retValue63 = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice and Name=@Name", Args: new { ListPrice = 0, Name = "Adjustable Race" });
                    var retValue64 = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice and Name=@Name", ListPrice: 0, Name: "Adjustable Race");

                    //LIMIT AND ORDERBY -> first row, last row
                    var retValue65 = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[]{"Red", "Black"});
                    var retValue66 = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID DESC", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });


                    //CLASSIC WAY
                    var retValue67 = aw.Query(CommandType.TableDirect, "Production", "Product");
                    var retValue68= aw.Query(commandText: "SELECT * FROM Production.Product");
                    var retValue69 = aw.Query(commandText: "SELECT * FROM Production.Product WHERE Name=@0", args: "Adjustable Race");
                    var retValue70 = aw.Query(commandText: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", args: new object[] { 0, "Adjustable Race" });
                    var retValue71 = aw.Query(commandText: "SELECT * FROM Sales.vSalesPerson").Select(f => new { FirstName = f.FirstName, LastName = f.LastName, PhoneNumber = f.PhoneNumber }).ToList();
                    var retValue72 = aw.Exists(commandText: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", args: new object[] { 0, "Adjustable Race" });
                    var retValue73 = aw.Exists(commandType: CommandType.TableDirect, schema: "Production", commandText: "Product", where: "ListPrice=@0 and Name=@1", args: new object[] { 0, "Adjustable Race" });
                }
            }
        }