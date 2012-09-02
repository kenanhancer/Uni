using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Data.Common;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using System.Linq.Expressions;

public enum DatabaseType { SQLServer, SqlServerCE, MySQL, Oracle, SQLite, PostgreSQL }
public static class UniExtensions
{
    public static dynamic RecordToExpando(this IDataReader rdr)
    {
        dynamic retValue = new ExpandoObject();
        var dict = retValue as IDictionary<string, object>;
        for (int i = 0; i < rdr.FieldCount; i++)
            dict.Add(rdr.GetName(i), DBNull.Value.Equals(rdr[i]) ? null : rdr[i]);
        return retValue;
    }
    public static dynamic ToExpando(this object obj)
    {
        if (obj.GetType() == typeof(ExpandoObject)) return obj;
        var result = new ExpandoObject();
        var dict = result as IDictionary<string, object>;
        int i = 0;
        if (obj.GetType() == typeof(NameValueCollection) || obj.GetType().IsSubclassOf(typeof(NameValueCollection)))
        {
            var nv = (NameValueCollection)obj;
            nv.Cast<string>().Select(key => new KeyValuePair<string, object>(key, nv[key])).ToList().ForEach(f => dict.Add(f));
        }
        else if (typeof(IEnumerable<dynamic>).IsAssignableFrom(obj.GetType()))
            obj.ToEnumerable<dynamic>().ToList().ForEach(f => dict.Add("Item" + (i++).ToString(), f));
        else if (typeof(IDictionary<string, object>).IsAssignableFrom(obj.GetType()))
            obj.ToDictionary().ToList().ForEach(f => dict.Add(f.Key, f.Value));
        else
            obj.GetType().GetProperties().ToList().ForEach(f => dict.Add(f.Name, f.GetValue(obj, null)));
        return result;
    }
    public static IDictionary<string, object> ToDictionary(this object obj)
    {
        if (typeof(Dictionary<string, object>).IsAssignableFrom(obj.GetType()))
            return (IDictionary<string, object>)obj;
        else
            return (IDictionary<string, object>)obj.ToExpando();
    }
    public static T To<T>(this object obj)
    {
        if (obj == DBNull.Value)
            return (T)default(T);
        else
            return (T)Convert.ChangeType(obj, typeof(T));
    }
    public static IEnumerable<T> ToEnumerable<T>(this object obj)
    {
        return obj as IEnumerable<T>;
    }
    public static T[] ToArray<T>(this object obj)
    {
        return ToEnumerable<T>(obj).ToArray();
    }
    public static string ToParameterString(this object obj, string parameterPrefix, string PKField = "", string parameterSuffix = "")
    {
        var retValue = string.Empty;
        dynamic dynamicObj = obj;
        var objType = obj.GetType();
        var sb = new StringBuilder();
        var isClass = !Enum.GetNames(typeof(TypeCode)).Contains(objType.Name);
        if (objType.IsArray)
            for (int x = 0; x < dynamicObj.Length; x++)
                sb.AppendFormat("{0}{1}{2}{3}", parameterPrefix, x, parameterSuffix, x < dynamicObj.Length - 1 ? "," : "");
        else if (string.IsNullOrEmpty(objType.Namespace) || isClass)
        {
            var dict = obj.ToDictionary();
            if (!string.IsNullOrEmpty(PKField))
                dict = obj.ToDictionary().Where(f => f.Key != PKField).ToDictionary(f => f.Key, f => f.Value);
            for (int x = 0; x < dict.Count; x++)
                if (dict.ElementAt(x).Value != null && dict.ElementAt(x).Value.GetType().IsArray)
                    for (int y = 0; y < ((object[])dict.ElementAt(x).Value).Length; y++)
                        sb.AppendFormat("{0}{1}{2}{3}", parameterPrefix, dict.ElementAt(x).Key + y.ToString(), parameterSuffix, x < dict.Count - 1 ? "," : "");
                else
                    sb.AppendFormat("{0}{1}{2}{3}", parameterPrefix, dict.ElementAt(x).Key, parameterSuffix, x < dict.Count - 1 ? "," : "");
        }
        else
            sb.AppendFormat("{0}{1}", parameterPrefix, "0");
        retValue = sb.ToString();
        return retValue;
    }
    public static string ToColumns(this object obj, string PKField = "")
    {
        return string.Format("{0}", string.Join(",", obj.ToDictionary().Keys.Where(f => f != PKField).ToArray()));
    }
    public static string ToColumnParameterString(this object obj, string parameterPrefix, string PKField = "", string parameterSuffix = "", string seperator = ",")
    {
        var columnParameterList = new List<string>();
        if (obj.GetType().IsArray)
            for (int i = 0; i < obj.ToArray<dynamic>().Length; i++)
                columnParameterList.Add(string.Format("{0}={1}{2}", PKField, parameterPrefix, i));
        else if (obj.ToDictionary().Count > 0)
            foreach (var item in obj.ToDictionary().Keys.Where(f => f != PKField))
                columnParameterList.Add(string.Format("{0}={1}{0}{2}", item, parameterPrefix, parameterSuffix));
        else
            columnParameterList.Add(string.Format("{0}={1}0", PKField, parameterPrefix));
        return string.Format("{0}", string.Join(seperator, columnParameterList.ToArray()));
    }
    public static Dictionary<string, object> ToParameters(this object obj, string parameterPrefix, string PKField = "", string parameterSuffix = "")
    {
        var retValue = new Dictionary<string, object>();
        if (obj == null) return retValue;
        dynamic dynamicObj = obj;
        var objType = obj.GetType();
        var isClass = !Enum.GetNames(typeof(TypeCode)).Contains(objType.Name);
        if (objType.IsArray)
            for (int x = 0; x < dynamicObj.Length; x++)
                retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, x, parameterSuffix), (dynamicObj[x].GetType() == typeof(ExpandoObject)) ? ((object)dynamicObj[x]).ToDictionary()[PKField] : dynamicObj[x]);
        else if (objType == typeof(Dictionary<string, object>) || string.IsNullOrEmpty(objType.Namespace) || isClass)
        {
            if (string.IsNullOrEmpty(objType.Namespace) || (isClass && !objType.IsGenericType))
                dynamicObj = obj.ToExpando();
            foreach (var item in dynamicObj)
                if (item.Value != null && item.Value.GetType().IsArray)
                {
                    dynamic argValueArray = item.Value;
                    var sb = new StringBuilder();
                    for (int y = 0; y < argValueArray.Length; y++)
                        retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, item.Key + y.ToString(), parameterSuffix), argValueArray[y]);
                }
                else
                    retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, item.Key, parameterSuffix), item.Value == null ? DBNull.Value : item.Value);
        }
        else
            retValue.Add(string.Format("{0}{1}", parameterPrefix, "0"), obj);
        return retValue;
    }
    public static IEnumerable<dynamic> ToCallBack(this DbCommand com)
    {
        foreach (var outputParameter in com.Parameters.Cast<DbParameter>().Where(f => f.Direction == ParameterDirection.Output))
        {
            dynamic ret = new ExpandoObject();
            ((IDictionary<string, object>)ret).Add(outputParameter.ParameterName, outputParameter.Value);
            yield return ret;
        }
    }
    public static Func<T1, T2> PropertyGetter<T1, T2>(T1 obj, string propertyName)
    {
        ParameterExpression param = Expression.Parameter(typeof(T1), "param");
        Expression GetPropertyValueExp = Expression.Lambda(Expression.Property(param, propertyName), param);
        Expression<Func<T1, T2>> GetPropertyValueLambda = (Expression<Func<T1, T2>>)GetPropertyValueExp;
        return GetPropertyValueLambda.Compile();
    }
    public static Func<object, object> PropertyGetter(object obj, string propertyName)
    {
        ParameterExpression param = Expression.Parameter(typeof(object), "param");
        Expression GetPropertyValueExp = Expression.Lambda(Expression.Property(param, propertyName), param);
        Expression<Func<object, object>> GetPropertyValueLambda = (Expression<Func<object, object>>)GetPropertyValueExp;
        return GetPropertyValueLambda.Compile();
    }
    public static Action<T1, T2> PropertySetter<T1, T2>(T1 obj, string propertyName)
    {
        PropertyInfo pi = typeof(T1).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        MethodInfo SetterMethodInfo = pi.GetSetMethod();
        ParameterExpression param = Expression.Parameter(typeof(T1), "param");
        ParameterExpression paramNewValue = Expression.Parameter(typeof(T2), "newValue");
        MethodCallExpression MethodCallSetterOfProperty = Expression.Call(param, SetterMethodInfo, paramNewValue);
        Expression SetPropertyValueExp = Expression.Lambda(MethodCallSetterOfProperty, param, paramNewValue);
        Expression<Action<T1, T2>> SetPropertyValueLambda = (Expression<Action<T1, T2>>)SetPropertyValueExp;
        return SetPropertyValueLambda.Compile();
    }
    public static Action<object, object> PropertySetter(object obj, string propertyName)
    {
        var type = obj.GetType();
        PropertyInfo pi = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        MethodInfo SetterMethodInfo = pi.GetSetMethod();

        ParameterExpression param = Expression.Parameter(typeof(object), "param");
        Expression convertedParam = Expression.Convert(param, type);

        ParameterExpression paramNewValue = Expression.Parameter(typeof(object), "newValue");
        Expression convertedParamNewValue = Expression.Convert(paramNewValue, pi.PropertyType);

        MethodCallExpression MethodCallSetterOfProperty = Expression.Call(convertedParam, SetterMethodInfo, convertedParamNewValue);

        Expression SetPropertyValueExp = Expression.Lambda(MethodCallSetterOfProperty, param, paramNewValue);
        Expression<Action<object, object>> SetPropertyValueLambda = (Expression<Action<object, object>>)SetPropertyValueExp;
        return SetPropertyValueLambda.Compile();
    }
    public static string WriteCsv<T>(this IEnumerable<T> list, Encoding encoding, string seperator = ",", bool writeHeadersToCSV = false)
    {
        string content = string.Empty;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (StreamWriter writer = new StreamWriter(memoryStream, encoding ?? Encoding.Default))
            {
                WriteToStream<T>(list, writer, seperator, writeHeadersToCSV);
                writer.Flush();
                memoryStream.Position = 0;
                using (StreamReader reader = new StreamReader(memoryStream, encoding ?? Encoding.Default))
                {
                    content = reader.ReadToEnd();
                    writer.Close();
                    reader.Close();
                    memoryStream.Close();
                }
            }
        }
        return content;
    }
    private static void WriteToStream<T>(IEnumerable<T> list, TextWriter writer, string seperator = ",", bool writeHeadersToCSV = false)
    {
        List<string> fields = typeof(T).GetProperties().Select(f => f.Name).ToList();
        if (fields.Count == 0)
            fields = Activator.CreateInstance<T>().ToDictionary().Keys.ToList();
        if (writeHeadersToCSV && fields.Count > 0)
            WriteRecord(fields, writer, seperator);
        for (int i = 0; i < list.Count(); i++)
        {
            T item = list.ElementAt(i);
            var itemDict = item.ToDictionary();
            if (i == 0 && writeHeadersToCSV && fields.Count == 0)
            {
                fields = itemDict.Keys.ToList();
                WriteRecord(fields, writer, seperator);
            }
            fields.Clear();
            fields.AddRange(itemDict.Values.Select(o => o == null ? "" : o.ToString()));
            WriteRecord(fields, writer, seperator);
        }
    }
    public static string WriteCsv(this DataTable dataTable, Encoding encoding, string seperator = ",", bool writeHeadersToCSV = false)
    {
        string content = string.Empty;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (StreamWriter writer = new StreamWriter(memoryStream, encoding ?? Encoding.Default))
            {
                WriteToStream(dataTable, writer, seperator, writeHeadersToCSV);
                writer.Flush();
                memoryStream.Position = 0;
                using (StreamReader reader = new StreamReader(memoryStream, encoding ?? Encoding.Default))
                {
                    content = reader.ReadToEnd();
                    writer.Close();
                    reader.Close();
                    memoryStream.Close();
                }
            }
        }
        return content;
    }
    private static void WriteToStream(DataTable dataTable, TextWriter writer, string seperator = ",", bool writeHeadersToCSV = false)
    {
        List<string> fields = (from DataColumn column in dataTable.Columns select column.ColumnName).ToList();
        if (writeHeadersToCSV)
            WriteRecord(fields, writer, seperator);
        foreach (DataRow row in dataTable.Rows)
        {
            fields.Clear();
            fields.AddRange(row.ItemArray.Select(o => o.ToString()));
            WriteRecord(fields, writer, seperator);
        }
    }
    private static void WriteRecord(IList<string> fields, TextWriter writer, string seperator = ",")
    {
        for (int i = 0; i < fields.Count; i++)
        {
            bool quotesRequired = fields[i].Contains(",");
            bool escapeQuotes = fields[i].Contains("\"");
            string fieldValue = (escapeQuotes ? fields[i].Replace("\"", "\"\"") : fields[i]);
            var ReplaceCarriageReturnsAndLineFeedsFromFieldValues = true;
            var CarriageReturnAndLineFeedReplacement = ",";
            var Seperator = seperator;
            if (ReplaceCarriageReturnsAndLineFeedsFromFieldValues && (fieldValue.Contains("\r") || fieldValue.Contains("\n")))
            {
                quotesRequired = true;
                fieldValue = fieldValue.Replace("\r\n", CarriageReturnAndLineFeedReplacement);
                fieldValue = fieldValue.Replace("\r", CarriageReturnAndLineFeedReplacement);
                fieldValue = fieldValue.Replace("\n", CarriageReturnAndLineFeedReplacement);
            }
            writer.Write(string.Format("{0}{1}{0}{2}", (quotesRequired || escapeQuotes ? "\"" : string.Empty), fieldValue, (i < (fields.Count - 1) ? Seperator : string.Empty)));
        }
        writer.WriteLine();
    }
}
public class Uni : DynamicObject
{
    DbProviderFactory _dbProviderFactory;
    ConnectionStringSettings conStrSettings;
    DatabaseType dbType;
    string parameterPrefix = "";
    string commandFormat = "{0}.{1}";
    string defaultSchema = "";
    public dynamic dyno;
    public Uni(string connectionStringName, DbProviderFactory dbProviderFactory = null)
    {
        SetBaseProperties(connectionStringName: connectionStringName, dbProviderFactory: dbProviderFactory);
    }
    public Uni(string connectionString, DatabaseType dbType, DbProviderFactory dbProviderFactory = null)
    {
        SetBaseProperties(connectionString: connectionString, dbType: dbType, dbProviderFactory: dbProviderFactory);
    }
    private void SetBaseProperties(string connectionStringName = "", string connectionString = "", DatabaseType dbType = DatabaseType.SQLServer, DbProviderFactory dbProviderFactory = null)
    {
        dyno = this;
        this.dbType = dbType;
        string providerName = string.Empty;
        if (!string.IsNullOrEmpty(connectionStringName))
        {
            conStrSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (conStrSettings.ProviderName.ToLower().Equals("System.Data.SqlClient".ToLower()))
                this.dbType = DatabaseType.SQLServer;
            else if (conStrSettings.ProviderName.ToLower().Equals("MySql.Data.MySqlClient".ToLower()))
                this.dbType = DatabaseType.MySQL;
            else if (conStrSettings.ProviderName.ToLower().Equals("Oracle.DataAccess.Client".ToLower()))
                this.dbType = DatabaseType.Oracle;
            else if (conStrSettings.ProviderName.ToLower().Equals("System.Data.SQLite".ToLower()))
                this.dbType = DatabaseType.SQLite;
            else if (conStrSettings.ProviderName.ToLower().Equals("Npgsql".ToLower()))
                this.dbType = DatabaseType.PostgreSQL;
            else if (conStrSettings.ProviderName.ToLower().Contains("SqlServerCe".ToLower()))
                this.dbType = DatabaseType.SqlServerCE;
        }
        if (this.dbType == DatabaseType.SQLServer || this.dbType == DatabaseType.SQLite)
        {
            providerName = "System.Data.SqlClient";
            parameterPrefix = "@";
            defaultSchema = "dbo";
            commandFormat = "[{0}].[{1}]";
        }
        else if (this.dbType == DatabaseType.MySQL)
        {
            providerName = "MySql.Data.MySqlClient";
            parameterPrefix = "?";
        }
        else if (this.dbType == DatabaseType.Oracle)
        {
            providerName = "Oracle.DataAccess.Client";
            parameterPrefix = ":";
        }
        else if (this.dbType == DatabaseType.SQLite)
            providerName = "System.Data.SQLite";
        else if (this.dbType == DatabaseType.PostgreSQL)
            providerName = "Npgsql";
        else if (this.dbType == DatabaseType.SqlServerCE)
            providerName = "System.Data.SqlServerCe.4.0";
        if (string.IsNullOrEmpty(connectionStringName))
            conStrSettings = new ConnectionStringSettings(name: "NewConnection", connectionString: connectionString, providerName: providerName);
        if (dbProviderFactory == null)
            _dbProviderFactory = DbProviderFactories.GetFactory(conStrSettings.ProviderName);
        else
            _dbProviderFactory = dbProviderFactory;
    }
    public virtual DbConnection NewConnection()
    {
        var con = _dbProviderFactory.CreateConnection();
        con.ConnectionString = conStrSettings.ConnectionString;
        con.Open();
        return con;
    }
    public virtual DbCommand NewCommand(CommandType commandType, string schema, string commandText, DbConnection con, params object[] args)
    {
        var com = _dbProviderFactory.CreateCommand();
        com.Connection = con;
        if (commandType == CommandType.TableDirect)
        {
            var tableName = string.IsNullOrEmpty(schema) ? commandText : string.Format(commandFormat, schema, commandText);
            com.CommandText = string.Format("SELECT * FROM {0}", tableName);
        }
        else if (commandType == CommandType.StoredProcedure)
        {
            var argumentArray = args.Length == 1 && args[0] != null && args[0].GetType().IsArray ? (object[])args[0] : args;
            com.CommandType = commandType;
            com.CommandText = string.IsNullOrEmpty(schema) ? commandText : string.Format(commandFormat, schema, commandText);
            if (argumentArray.Length > 0)
            {
                var parameterList = new List<DbParameter>();
                var comParameters = GetCommandParameters(commandText, schema);
                for (int i = 0; i < comParameters.Count(); i++)
                {
                    if (comParameters.ElementAt(i).PARAMETER_MODE == "IN")
                    {
                        var parameterName = comParameters.ElementAt(i).PARAMETER_NAME;
                        DbParameter dbParameter = com.CreateParameter();
                        dbParameter.ParameterName = this.dbType == DatabaseType.Oracle || this.dbType == DatabaseType.MySQL ? string.Format("{0}{1}", parameterPrefix, parameterName) : parameterName;
                        dbParameter.Value = typeof(Dictionary<string, object>).IsAssignableFrom(argumentArray[0].GetType()) ? argumentArray[0].ToDictionary().Values.ElementAt(i) : argumentArray[i];
                        parameterList.Add(dbParameter);
                    }
                    else if (comParameters.ElementAt(i).PARAMETER_MODE == "OUT")
                    {
                        var parameterName = comParameters.ElementAt(i).PARAMETER_NAME;
                        DbParameter dbParameter = com.CreateParameter();
                        //dbParameter.ParameterName = this.dbType == DatabaseType.Oracle || this.dbType == DatabaseType.MySQL ? string.Format("{0}{1}", parameterPrefix, parameterName) : parameterName;
                        dbParameter.ParameterName = parameterName;
                        dbParameter.Direction = ParameterDirection.Output;
                        parameterList.Add(dbParameter);
                    }
                }
                com.Parameters.AddRange(parameterList.ToArray());
            }
        }
        else if (commandType == CommandType.Text)
        {
            com.CommandType = commandType;
            com.CommandText = commandText;
            Regex parameterRegex = new Regex(string.Format("(?<parameter>{0}[^,;) ]+)", parameterPrefix == "?" ? @"\?" : parameterPrefix));
            MatchCollection parameterMatchCollection = parameterRegex.Matches(commandText);
            string[] parameterArray = parameterMatchCollection.Cast<Match>().Select(f => f.Groups["parameter"].Value).Distinct().ToArray();
            if (args != null && args.Length > 0 && args[0] != null)
            {
                var argDict = new Dictionary<string, object>();
                if (args[0].GetType().IsPrimitive)
                    argDict = args.ToParameters(parameterPrefix);
                else if (args[0].GetType() != typeof(Dictionary<string, object>))
                    argDict = args[0].ToParameters(parameterPrefix);
                else
                    argDict = (Dictionary<string, object>)args[0];
                if (parameterArray.Length > 0)
                {
                    for (int i = 0; i < parameterArray.Length; i++)
                    {
                        var paramValue = argDict.Where(f => f.Key == parameterArray[i]).SingleOrDefault().Value;
                        com.Parameters.Add(NewParameter(com, parameterArray[i], paramValue));
                    }
                }
                else
                    if (args != null && argDict != null && argDict.Count > 0)
                        for (int i = 0; i < args.Length; i++)
                            com.Parameters.Add(NewParameter(com, string.Format("{0}{1}", parameterPrefix, i), args[i]));
            }
        }
        return com;
    }
    public virtual DbParameter NewParameter(DbCommand com, string parameterName, object value)
    {
        DbParameter parameter = com.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value == null ? DBNull.Value : value;
        return parameter;
    }
    public virtual IEnumerable<dynamic> GetCommandParameters(string commandText, string schema = "")
    {
        IEnumerable<dynamic> retValue = null;
        using (var con = NewConnection())
        {
            var sql = "";
            if (this.dbType == DatabaseType.SQLServer || this.dbType == DatabaseType.MySQL)
            {
                schema = string.IsNullOrEmpty(schema) ? defaultSchema : schema;
                sql = string.Format("SELECT PARAMETER_MODE,PARAMETER_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH DATA_LENGTH FROM INFORMATION_SCHEMA.PARAMETERS WHERE {0}", string.IsNullOrEmpty(schema) ? string.Format("SPECIFIC_NAME={0}0", parameterPrefix) : string.Format("SPECIFIC_SCHEMA={0}0 AND SPECIFIC_NAME={0}1", parameterPrefix));
            }
            else if (this.dbType == DatabaseType.Oracle)
                sql = string.Format("SELECT ARGUMENT_NAME PARAMETER_NAME, IN_OUT PARAMETER_MODE, DATA_TYPE, DATA_LENGTH FROM SYS.ALL_ARGUMENTS WHERE {0} ORDER BY POSITION", string.IsNullOrEmpty(schema) ? "OBJECT_NAME=:0" : "OWNER=:0 AND OBJECT_NAME=:1");
            //else if (this.dbType == DatabaseType.SQLite)
            //else if (this.dbType == DatabaseType.PostgreSQL)
            if (string.IsNullOrEmpty(schema))
                retValue = Query(commandText: sql, args: new object[] { commandText }.ToParameters(parameterPrefix));
            else
                retValue = Query(commandText: sql, args: new object[] { schema, commandText }.ToParameters(parameterPrefix));
        }
        return retValue;
    }
    public virtual IEnumerable<dynamic> GetTables()
    {
        IEnumerable<dynamic> retValue = null;
        using (var con = NewConnection())
        {
            if (this.dbType == DatabaseType.SQLServer)
                retValue = Query(commandText: "SELECT * FROM [sys].[all_objects] WHERE type_desc LIKE '%table%'");
            else if (this.dbType == DatabaseType.MySQL)
                retValue = Query(commandText: "SELECT * FROM INFORMATION_SCHEMA.TABLES");
            else if (this.dbType == DatabaseType.Oracle)
                retValue = Query(commandText: "SELECT * FROM all_tables");
            else if (this.dbType == DatabaseType.SQLite)
                retValue = Query(commandText: "SELECT * FROM sqlite_master WHERE type='table'");
            //else if (this.dbType == DatabaseType.PostgreSQL)
        }
        return retValue;
    }
    public virtual IEnumerable<dynamic> GetColumns(string commandText, string schema = "")
    {
        IEnumerable<dynamic> retValue = null;
        using (var con = NewConnection())
        {
            var sql = "";
            if (this.dbType == DatabaseType.SQLServer)
            {
                sql = string.Format("SELECT r2.name COLUMN_NAME FROM [sys].[all_objects] r1, [sys].[all_columns] r2 WHERE r1.object_id=r2.object_id AND schema_name(r1.schema_id) like {0}0 AND r1.name={0}1", parameterPrefix);
                schema = string.IsNullOrEmpty(schema) ? defaultSchema : schema;
                retValue = Query(commandText: sql, args: new object[] { schema, commandText });
            }
            else if (this.dbType == DatabaseType.MySQL)
            {
                sql = string.Format("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {0}0", parameterPrefix);
                retValue = Query(commandText: sql, args: new object[] { commandText });
            }
            else if (this.dbType == DatabaseType.Oracle)
            {
                sql = string.Format("SELECT COLUMN_NAME FROM USER_TAB_COLUMNS WHERE TABLE_NAME = {0}0", parameterPrefix);
                retValue = Query(commandText: sql, args: new object[] { commandText });

            }
            else if (this.dbType == DatabaseType.SQLite)
            {
                retValue = Query(commandText: string.Format("PRAGMA table_info( {0} )", commandText));
            }
            //else if (this.dbType == DatabaseType.PostgreSQL)
        }
        return retValue;
    }
    public virtual dynamic NewExpando(string commandText, string schema = "")
    {
        dynamic result = new ExpandoObject();
        var columns = GetColumns(commandText, schema);
        foreach (dynamic column in columns)
            ((IDictionary<string, object>)result).Add(column.COLUMN_NAME, null);
        return result;
    }
    public virtual IEnumerable<dynamic> Query(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", Action<dynamic> callBack = null, params object[] args)
    {
        var retValue = new List<dynamic>();
        using (var con = NewConnection())
        {
            var com = NewCommand(commandType, schema, commandText, con, args);
            var reader = com.ExecuteReader();
            if (callBack != null)
                callBack.Invoke(new { SqlQuery = commandText, OutputParameters = com.ToCallBack().ToList() });
            int i = 0;
            do
            {
                //yield return ResultSet(i++, commandType, schema, commandText, args);
                retValue.Add(ResultSet(i++, commandType, schema, commandText, args));
            } while (reader.NextResult());
        }
        if (retValue.Count == 1)
            return retValue[0];
        else
            return retValue;
    }
    public virtual IEnumerable<dynamic> Query<T>(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", Action<dynamic> callBack = null, params object[] args)
    {
        var retValue = new List<dynamic>();
        using (var con = NewConnection())
        {
            var com = NewCommand(commandType, schema, commandText, con, args);
            var reader = com.ExecuteReader();
            if (callBack != null)
                callBack.Invoke(new { SqlQuery = commandText, OutputParameters = com.ToCallBack().ToList() });
            int i = 0;
            do
            {
                //yield return ResultSet(i++, commandType, schema, commandText, args);
                retValue.Add(ResultSet<T>(i++, commandType, schema, commandText, args));
            } while (reader.NextResult());
        }
        if (retValue.Count == 1)
            return retValue[0];
        else
            return retValue;
    }
    private IEnumerable<dynamic> ResultSet(int index, CommandType commandType = CommandType.Text, string schema = "", string commandText = "", params object[] args)
    {
        using (var con = NewConnection())
        {
            var com = NewCommand(commandType, schema, commandText, con, args);
            var reader = com.ExecuteReader();
            int i = 0;
            do
            {
                if (i++ == index)
                    while (reader.Read())
                        yield return reader.RecordToExpando();
            } while (reader.NextResult());
        }
    }
    private IEnumerable<T> ResultSet<T>(int index, CommandType commandType = CommandType.Text, string schema = "", string commandText = "", params object[] args)
    {
        using (var con = NewConnection())
        {
            var com = NewCommand(commandType, schema, commandText, con, args);
            var reader = com.ExecuteReader();
            int i = 0;
            var propertyList = typeof(T).GetProperties().ToList();
            var setterList = new Dictionary<string, Action<object, object>>();
            var columnList = new List<string>();
            dynamic retValue = Activator.CreateInstance<T>();
            propertyList.ForEach(pi => setterList.Add(pi.Name, UniExtensions.PropertySetter(retValue, pi.Name)));
            do
            {
                if (i++ == index)
                    columnList.Clear();
                    while (reader.Read())
                    {
                        if (Enum.GetNames(typeof(TypeCode)).Contains(typeof(T).Name))
                            retValue = reader.IsDBNull(0) ? default(T) : reader[0];
                        else
                        {
                            retValue = Activator.CreateInstance<T>();
                            if(columnList.Count==0)
                                columnList = reader.GetSchemaTable().AsEnumerable().Select(f => f["ColumnName"].ToString()).ToList();
                            propertyList.ForEach(pi =>
                                {
                                    if (columnList.Contains(pi.Name))
                                    {
                                        int columnOrdinal = reader.GetOrdinal(pi.Name);
                                        setterList[pi.Name](retValue, reader.IsDBNull(columnOrdinal) ? null : reader[columnOrdinal]);
                                    }
                                    else
                                        setterList[pi.Name](retValue, null);
                                }
                            );
                        }
                        yield return retValue;
                    }
            } while (reader.NextResult());
        }
    }
    public virtual int Count(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", string where = "", Action<dynamic> callBack = null, params object[] args)
    {
        if (commandType == CommandType.Text)
            return this.dyno.Count(Sql: commandText, CallBack: callBack, Args: args);
        else
            return this.dyno.Count(Schema: schema, Table: commandText, Where: where, CallBack: callBack, Args: args);
    }
    public virtual decimal Sum(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", string where = "", string columns = "", Action<dynamic> callBack = null, params object[] args)
    {
        if (commandType == CommandType.Text)
            return this.dyno.Sum(Sql: commandText, Columns: columns, CallBack: callBack, Args: args);
        else
            return this.dyno.Sum(Schema: schema, Table: commandText, Where: where, Columns: columns, CallBack: callBack, Args: args);
    }
    public virtual decimal Max(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", string where = "", string columns = "", Action<dynamic> callBack = null, params object[] args)
    {
        if (commandType == CommandType.Text)
            return this.dyno.Max(Sql: commandText, Columns: columns, CallBack: callBack, Args: args);
        else
            return this.dyno.Max(Schema: schema, Table: commandText, Where: where, Columns: columns, CallBack: callBack, Args: args);
    }
    public virtual decimal Min(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", string where = "", string columns = "", Action<dynamic> callBack = null, params object[] args)
    {
        if (commandType == CommandType.Text)
            return this.dyno.Min(Sql: commandText, Columns: columns, CallBack: callBack, Args: args);
        else
            return this.dyno.Min(Schema: schema, Table: commandText, Where: where, Columns: columns, CallBack: callBack, Args: args);
    }
    public virtual decimal Avg(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", string where = "", string columns = "", Action<dynamic> callBack = null, params object[] args)
    {
        if (commandType == CommandType.Text)
            return this.dyno.Avg(Sql: commandText, Columns: columns, CallBack: callBack, Args: args);
        else
            return this.dyno.Avg(Schema: schema, Table: commandText, Where: where, Columns: columns, CallBack: callBack, Args: args);
    }
    public virtual bool Exists(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", string where = "", Action<dynamic> callBack = null, params object[] args)
    {
        if (commandType == CommandType.Text)
            return this.dyno.Exists(Sql: commandText, CallBack: callBack, Args: args);
        else
            return this.dyno.Exists(Schema: schema, Table: commandText, Where: where, CallBack: callBack, Args: args);
    }
    public virtual object ExecuteScalar(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", Action<dynamic> callBack = null, params object[] args)
    {
        var retValue = default(object);
        using (var con = NewConnection())
        {
            var com = NewCommand(commandType, schema, commandText, con, args);
            retValue = com.ExecuteScalar();
            if (callBack != null)
                callBack.Invoke(new { SqlQuery = com.CommandText, OutputParameters = com.ToCallBack().ToList() });
        }
        return retValue;
    }
    public virtual T ExecuteScalar<T>(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", Action<dynamic> callBack = null, params object[] args)
    {
        object outputs = null;
        var getValue = ExecuteScalar(commandType: commandType, schema: schema, commandText: commandText, callBack: (Action<object>)(f => outputs = f), args: args);
        if (callBack != null)
            callBack.Invoke(outputs);
        return getValue.To<T>();
    }
    public virtual object[] ExecuteScalar(IEnumerable<DbCommand> commands)
    {
        var retValue = new List<object>();
        using (var con = NewConnection())
        {
            using (var dbTransaction = con.BeginTransaction())
            {
                foreach (var cmd in commands)
                {
                    cmd.Connection = con;
                    cmd.Transaction = dbTransaction;
                    retValue.Add(cmd.ExecuteScalar());
                }
                dbTransaction.Commit();
            }
        }
        return retValue.ToArray();
    }
    public virtual object ExecuteScalar(DbCommand command)
    {
        var getValue = ExecuteScalar(new DbCommand[] { command });
        return getValue != null ? getValue[0] : null;
    }
    public virtual int ExecuteNonQuery(CommandType commandType = CommandType.Text, string schema = "", string commandText = "", params object[] args)
    {
        var retValue = 0;
        using (var con = NewConnection())
        {
            var com = NewCommand(commandType, schema, commandText, con, args);
            retValue = ExecuteNonQuery(com);
        }
        return retValue;
    }
    public virtual int ExecuteNonQuery(IEnumerable<DbCommand> commands)
    {
        var retValue = 0;
        using (var con = NewConnection())
        {
            using (var dbTransaction = con.BeginTransaction())
            {
                foreach (var cmd in commands)
                {
                    cmd.Connection = con;
                    cmd.Transaction = dbTransaction;
                    retValue += cmd.ExecuteNonQuery();
                }
                dbTransaction.Commit();
            }
        }
        return retValue;
    }
    public virtual int ExecuteNonQuery(DbCommand command)
    {
        return ExecuteNonQuery(new DbCommand[] { command });
    }
    private dynamic CallQueryReflection(Type type, CommandType commandType = CommandType.Text, string schema = "", string commandText = "", Action<dynamic> callBack = null, params object[] args)
    {
        var mi = typeof(Uni).GetMethods().Where(f => f.Name == "Query" && f.IsGenericMethod).SingleOrDefault();
        mi = mi.MakeGenericMethod(type);
        dynamic queryResult = mi.Invoke(this, new object[] { commandType, schema, commandText, callBack, args });
        return queryResult;
    }
    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        result = null;
        var csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
        var typeArgs = csharpBinder.GetProperty("TypeArguments").GetValue(binder, null) as IList<Type>;
        var binderName = binder.Name.ToLower();
        var arguments = new Dictionary<string, object>();
        var queries = new Dictionary<string, object>();
        var commandArgs = new Dictionary<string, object>();
        var argDict = new Dictionary<string, string>();
        var callBack = (Action<dynamic>)null;
        string schema = "", table = "", fn = "", sp = "", where = "", orderby = "", groupby = "", having = "", columns = "*", sql = "", fullTableName = "", fullFnName = "", fullSpName = "", PKField = "", rowNumberColumn = "";
        int limit = 0, pageSize = 0, pageNo = 0;
        for (int i = 0; i < args.Length; i++)
        {
            var argumentName = binder.CallInfo.ArgumentNames[i].ToLower();
            dynamic argumentValue = args[i];
            arguments.Add(argumentName, argumentValue);
            if (argumentName == "schema")
                schema = argumentValue.ToString();
            else if (argumentName == "table")
                table = argumentValue.ToString();
            else if (argumentName == "fn")
                fn = argumentValue.ToString();
            else if (argumentName == "sp")
                sp = argumentValue.ToString();
            else if (argumentName == "where")
                where = argumentValue.ToString();
            else if (argumentName == "orderby")
                orderby = argumentValue.ToString();
            else if (argumentName == "groupby")
                groupby = argumentValue.ToString();
            else if (argumentName == "having")
                having = argumentValue.ToString();
            else if (argumentName == "columns")
                columns = argumentValue.ToString();
            else if (argumentName == "limit")
                limit = argumentValue;
            else if (argumentName == "pagesize")
                pageSize = argumentValue;
            else if (argumentName == "pageno")
                pageNo = argumentValue;
            else if (argumentName == "sql")
                sql = argumentValue.ToString();
            else if (argumentName == "callback")
                callBack = argumentValue;
            else if (argumentName == "pkfield")
                PKField = argumentValue;
            else if (argumentName == "rownumbercolumn")
                rowNumberColumn = argumentValue;
            else if (argumentName != "args")
            {
                commandArgs.Add(binder.CallInfo.ArgumentNames[i], argumentValue);
                if (argumentValue != null && argumentValue.GetType().IsArray)
                {
                    var sb = new StringBuilder();
                    dynamic argValueArray = argumentValue;
                    for (int y = 0; y < argValueArray.Length; y++)
                        sb.AppendFormat("{0}{1}{2}", parameterPrefix, binder.CallInfo.ArgumentNames[i] + y.ToString(), y < argValueArray.Length - 1 ? "," : "");
                    argDict.Add(string.Format("{0}{1}", parameterPrefix, binder.CallInfo.ArgumentNames[i]), sb.ToString());
                }
            }
        }
        var columnArray = columns.Split(',');
        if (string.IsNullOrEmpty(sql))
            if (!string.IsNullOrEmpty(table))
                fullTableName = string.IsNullOrEmpty(schema) ? table : string.Format(commandFormat, schema, table);
            else if (!string.IsNullOrEmpty(fn))
                fullFnName = string.IsNullOrEmpty(schema) ? string.Format("{0} ({1})", fn, commandArgs.Count > 0 ? commandArgs.ToParameterString(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameterString(parameterPrefix) : "") : string.Format(commandFormat + " ({2})", schema, fn, arguments.ContainsKey("args") ? arguments["args"].ToParameterString(parameterPrefix) : "");
            else if (!string.IsNullOrEmpty(sp))
                fullSpName = string.IsNullOrEmpty(schema) ? sp : string.Format(commandFormat, schema, sp);
        object outputs = null;
        object queryResult = null;
        if (binderName == "query" || binderName == "exists" || binderName == "count" || binderName == "sum" || binderName == "max" || binderName == "min" || binderName == "avg")
        {
            if (!string.IsNullOrEmpty(sp))
                if (typeArgs.Count > 0)
                    queryResult = CallQueryReflection(typeArgs[0], CommandType.StoredProcedure, schema, sp, (Action<dynamic>)(f => outputs = f), commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
                else
                    queryResult = Query(commandType: CommandType.StoredProcedure, schema: schema, commandText: sp, callBack: (Action<dynamic>)(f => outputs = f), args: commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
            else
            {
                if (limit > 0)
                    if (this.dbType == DatabaseType.Oracle)
                        where = string.IsNullOrEmpty(where) ? string.Format("ROWNUM<={0}", limit) : string.Format("{0} AND ROWNUM<={1}", where, limit);
                    else if (this.dbType == DatabaseType.SQLServer)
                        columns = string.Format("TOP {0} {1}", limit, columns);
                if (commandArgs.Count > 0 && string.IsNullOrEmpty(where))
                    where = string.Join(" AND ", commandArgs.Where(f => f.Value != null && !f.Value.GetType().IsArray).Select(f => string.Format("{0}={1}", f.Key, string.Format("{0}{1}", parameterPrefix, f.Key))).ToArray());
                if (binderName == "count" || binderName == "sum" || binderName == "max" || binderName == "min" || binderName == "avg")
                {
                    var sb = new StringBuilder();
                    if (columnArray.Length > 1)
                    {
                        for (int i = 0; i < columnArray.Length; i++)
                            sb.AppendFormat("{0}({1}) {2}", binderName.ToUpper(), columnArray[i], i < columnArray.Length - 1 ? string.Format("{0}{1},", binderName.ToUpper(), columnArray[i]) : string.Format("{0}{1}", binderName.ToUpper(), columnArray[i]));
                        columns = sb.ToString();
                    }
                    else
                        columns = string.Format("{0}({1})", binderName.ToUpper(), binderName == "count" && string.IsNullOrEmpty(columns) ? "*" : columns);
                    if (!string.IsNullOrEmpty(sql))
                        sql = string.Format("SELECT {0} FROM ({1}) t", columns, sql);
                }
                if (arguments.ContainsKey("args") && arguments["args"] != null)
                {
                    var objType = arguments["args"].GetType();
                    if (string.IsNullOrEmpty(objType.Namespace))
                    {
                        var dict = arguments["args"].ToDictionary();
                        for (int x = 0; x < dict.Count; x++)
                            if (dict.ElementAt(x).Value.GetType().IsArray)
                            {
                                var sb = new StringBuilder();
                                dynamic argValueArray = dict.ElementAt(x).Value;
                                for (int y = 0; y < argValueArray.Length; y++)
                                    sb.AppendFormat("{0}{1}{2}", parameterPrefix, dict.ElementAt(x).Key + y.ToString(), y < argValueArray.Length - 1 ? "," : "");
                                argDict.Add(string.Format("{0}{1}", parameterPrefix, dict.ElementAt(x).Key), sb.ToString());
                            }
                    }
                }
                if (argDict.Count > 0)
                    foreach (var item in argDict)
                        where = where.Replace(item.Key, string.Format("({0})", item.Value));
                if (string.IsNullOrEmpty(sql))
                    sql = string.Format("SELECT {0} FROM {1}{2}{3}{4}{5}{6}", columns, string.IsNullOrEmpty(table) ? fullFnName : fullTableName, string.IsNullOrEmpty(where) ? "" : string.Format(" WHERE {0}", where), string.IsNullOrEmpty(groupby) ? "" : string.Format(" GROUP BY {0}", groupby), string.IsNullOrEmpty(having) ? "" : string.Format(" HAVING {0}", having), string.IsNullOrEmpty(orderby) ? "" : string.Format(" ORDER BY {0}", orderby), limit > 0 && (this.dbType == DatabaseType.MySQL || this.dbType == DatabaseType.SQLite) ? string.Format(" LIMIT {0}", limit) : "");
                if (binderName == "exists")
                {
                    sql = string.Format("SELECT CASE WHEN EXISTS({0}) THEN 1 ELSE 0 END as RESULT {1}", sql, this.dbType == DatabaseType.Oracle ? "FROM DUAL" : null);
                    queryResult = ExecuteScalar<bool>(commandType: CommandType.Text, commandText: sql, callBack: (Action<dynamic>)(f => outputs = f), args: commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
                }
                else if (binderName == "count")
                    queryResult = ExecuteScalar<int>(commandType: CommandType.Text, commandText: sql, callBack: (Action<dynamic>)(f => outputs = f), args: commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
                else if ((binderName == "sum" || binderName == "max" || binderName == "min" || binderName == "avg") && columnArray.Length == 1)
                    queryResult = ExecuteScalar<decimal>(commandType: CommandType.Text, commandText: sql, callBack: (Action<dynamic>)(f => outputs = f), args: commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
                else
                {
                    if (pageSize > 0 || pageNo > 0)
                    {
                        if (pageNo <= 0) pageNo = 1;
                        if (pageSize <= 0) pageSize = 10;
                        var pageStart = (pageNo - 1) * pageSize;
                        if (this.dbType == DatabaseType.SQLServer)
                            sql = string.Format("SELECT * FROM (SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber, * FROM ({1}) as PagedTable) as PagedRecords WHERE RowNumber > {2} AND RowNumber <={3}", rowNumberColumn, sql, pageStart, (pageStart + pageSize));
                        else if (this.dbType == DatabaseType.Oracle)
                            sql = string.Format("SELECT * FROM (SELECT T1.*,ROWNUM ROWNUMBER FROM ({0}) T1) WHERE ROWNUMBER > {1} and ROWNUMBER <= {2}", sql, pageStart, (pageStart + pageSize));
                        else if (this.dbType == DatabaseType.MySQL || this.dbType == DatabaseType.SQLite)
                            sql = string.Format("{0} LIMIT {1},{2}", sql, pageStart, pageSize);
                    }
                    if (typeArgs.Count > 0)
                        queryResult = CallQueryReflection(typeArgs[0], CommandType.Text, "", sql, (Action<dynamic>)(f => outputs = f), commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
                    else
                        queryResult = Query(commandType: CommandType.Text, commandText: sql, callBack: (Action<dynamic>)(f => outputs = f), args: commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
                }
            }
            if (limit == 1)
                if (columnArray.Length == 1 && columnArray[0] != "*")
                    result = queryResult.ToEnumerable<object>().SingleOrDefault().ToDictionary()[columnArray[0]];
                else
                    result = queryResult.ToEnumerable<dynamic>().SingleOrDefault();
            else
                result = queryResult;
        }
        else if (binderName == "insert")
        {
            var newIDSql = "";
            if (this.dbType == DatabaseType.SQLServer)
                newIDSql = ";SELECT SCOPE_IDENTITY()";
            else if (this.dbType == DatabaseType.MySQL)
                newIDSql = ";SELECT LAST_INSERT_ID()";
            else if (this.dbType == DatabaseType.SQLite)
                newIDSql = ";SELECT LAST_INSERT_ROWID();";
            if (arguments.ContainsKey("args") && arguments["args"].GetType().IsArray)
            {
                var commandList = new List<DbCommand>();
                foreach (var item in ((object[])arguments["args"]))
                {
                    sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2}){3}", fullTableName, item.ToColumns(PKField), item.ToParameterString(parameterPrefix, PKField), newIDSql);
                    commandList.Add(NewCommand(CommandType.Text, "", sql, null, item.ToParameters(parameterPrefix)));
                }
                result = ExecuteScalar(commandList.ToArray());
            }
            else
            {
                if (string.IsNullOrEmpty(sql))
                    sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2}){3}", fullTableName, arguments["args"].ToColumns(PKField), arguments["args"].ToParameterString(parameterPrefix, PKField), newIDSql);
                result = ExecuteScalar<int>(commandType: CommandType.Text, commandText: sql, args: commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null);
            }
        }
        else if (binderName == "bulkinsert")
        {
            if (arguments.ContainsKey("args") && arguments["args"].GetType().IsArray)
            {
                var commandList = new List<DbCommand>();
                var parameterList = new Dictionary<string, object>();
                var argumentArray = ((object[])arguments["args"]);
                var sqlList = new List<string>();
                for (int i = 0; i < argumentArray.Length; i++)
                {
                    var item = argumentArray[i];
                    sqlList.Add(string.Format("INSERT INTO {0} ({1}) VALUES ({2})", fullTableName, item.ToColumns(PKField), item.ToParameterString(parameterPrefix, PKField, i.ToString())));
                    var getParameters = item.ToParameters(parameterPrefix: parameterPrefix, parameterSuffix: i.ToString());
                    getParameters.ToList().ForEach(f => parameterList.Add(f.Key, f.Value));
                }
                result = ExecuteNonQuery(commandType: CommandType.Text, commandText: string.Join(";", sqlList.ToArray()), args: parameterList);
            }
        }
        else if (binderName == "update")
        {
            if (arguments.ContainsKey("args") && arguments["args"].GetType().IsArray)
            {
                var commandList = new List<DbCommand>();
                foreach (var item in ((object[])arguments["args"]))
                {
                    sql = string.Format("UPDATE {0} SET {1} {2}", fullTableName, item.ToColumnParameterString(parameterPrefix, PKField), string.Format("WHERE {0}={1}{0}", PKField, parameterPrefix));
                    var parameterList = item.ToParameters(parameterPrefix);
                    commandList.Add(NewCommand(CommandType.Text, "", sql, null, parameterList));
                }
                result = ExecuteNonQuery(commandList.ToArray());
            }
            else
            {
                if (string.IsNullOrEmpty(sql))
                    sql = string.Format("UPDATE {0} SET {1} {2}", fullTableName, arguments["args"].ToColumnParameterString(parameterPrefix, PKField), string.Format("WHERE {0}={1}{0}", PKField, parameterPrefix));
                var parameterList = commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null;

                result = ExecuteNonQuery(commandType: CommandType.Text, commandText: sql, args: parameterList);
            }
        }
        else if (binderName == "bulkupdate")
        {
            if (arguments.ContainsKey("args") && arguments["args"].GetType().IsArray)
            {
                var commandList = new List<DbCommand>();
                var parameterList = new Dictionary<string, object>();
                var argumentArray = ((object[])arguments["args"]);
                var sqlList = new List<string>();
                for (int i = 0; i < argumentArray.Length; i++)
                {
                    var item = argumentArray[i];
                    sqlList.Add(string.Format("UPDATE {0} SET {1} {2}", fullTableName, item.ToColumnParameterString(parameterPrefix, PKField, i.ToString()), string.Format("WHERE {0}={1}{0}{2}", PKField, parameterPrefix, i.ToString())));
                    var getParameters = item.ToParameters(parameterPrefix: parameterPrefix, parameterSuffix: i.ToString());
                    getParameters.ToList().ForEach(f => parameterList.Add(f.Key, f.Value));
                }
                result = ExecuteNonQuery(commandType: CommandType.Text, commandText: string.Join(";", sqlList.ToArray()), args: parameterList);
            }
        }
        else if (binderName == "delete")
        {
            if (arguments.ContainsKey("args") && arguments["args"].GetType().IsArray)
            {
                where = ((object[])arguments["args"]).ToColumnParameterString(parameterPrefix: parameterPrefix, PKField: PKField, seperator: " OR ");
                sql = string.Format("DELETE FROM {0} WHERE {1}", fullTableName, where);
                var parameterList = ((object[])arguments["args"]).ToParameters(parameterPrefix: parameterPrefix, PKField: PKField);
                result = ExecuteNonQuery(NewCommand(CommandType.Text, "", sql, null, parameterList));
            }
            else
            {
                if (string.IsNullOrEmpty(sql))
                {
                    where = commandArgs.Count > 0 ? commandArgs.ToColumnParameterString(parameterPrefix: parameterPrefix, PKField: PKField, seperator: " AND ") : arguments.ContainsKey("args") ? arguments["args"].ToColumnParameterString(parameterPrefix: parameterPrefix, PKField: PKField, seperator: " OR ") : null;
                    sql = string.Format("DELETE FROM {0} WHERE {1}", fullTableName, where);
                }
                var parameterList = commandArgs.Count > 0 ? commandArgs.ToParameters(parameterPrefix) : arguments.ContainsKey("args") ? arguments["args"].ToParameters(parameterPrefix) : null;
                result = ExecuteNonQuery(commandType: CommandType.Text, commandText: sql, args: parameterList);
            }
        }
        if (callBack != null)
            callBack.Invoke(outputs);
        return true;
    }
}