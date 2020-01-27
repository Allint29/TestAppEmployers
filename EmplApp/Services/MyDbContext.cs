using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmplApp.Models;

namespace EmplApp.Services
{
    /// <summary>
    /// Класс соединеия с базой данных
    /// </summary>
    public class MyDbContext
    {
        private string _connectionString { get; set; }
        private string _nameDatabase { get; set; } = "MyDefaultDatabase";
        private string _nameServer { get; set; }
        private string _nameCatalog { get; set; }
        private SqlConnection _myConn { get; set; }
        private bool _connected = false;

        public List<Employee> Employers { get; set; } = new List<Employee>();
        private readonly List<Type> UsingTypes = new List<Type>();
        private readonly List<string> UsingTypesString = new List<string>() { "System.Int32", "System.String", "System.DateTime", "System.Boolean" };

        public MyDbContext() { }

        private bool ContainsIntegratedSecurityString(string str)
        {
            if (!String.IsNullOrEmpty(_connectionString) && _connectionString.Contains("Integrated security") && _connectionString.Contains("SSPI"))
            {
                return true;
            }
            Debug.WriteLine($"{str}: Строка подключения к БД должна содержать 'Integrated security=SSPI;'");
            return false;
        }

        private bool IsConnected(string str)
        {
            if (_connected)
            {
                Debug.WriteLine(str + ": подключена БД");
                return true;
            }
            Debug.WriteLine(str + ": НЕ подключена БД");
            return false;

        }

        public async Task CreateNewDatabase(string connectionString)
        {
            if (IsConnected("CreateNewDatabase")) return;

            var listConnectionString = connectionString.Split(";");
            var conectionStringBuilder = new StringBuilder();
            foreach (var item in listConnectionString)
            {
                if (item.ToLowerInvariant().Contains("database") && item.Contains("="))
                {
                    var item_list = item.Split("=");
                    _nameDatabase = item_list[item_list.Length - 1].Trim();
                }
                else if (item.ToLowerInvariant().Contains("integrated security") && item.Contains("="))
                {
                    // MessageBox.Show(item);
                    conectionStringBuilder.Append(item + ";");
                }
                else if (item.ToLowerInvariant().Contains("server") && item.Contains("="))
                {
                    var item_list = item.Split("=");
                    _nameServer = item_list[item_list.Length - 1].Trim();

                    conectionStringBuilder.Append(item + ";");
                }


            }

            _nameCatalog = $"Initial Catalog={_nameDatabase};";
            _connectionString = conectionStringBuilder.ToString();

            if (!ContainsIntegratedSecurityString("CreateNewDatabase")) return;


            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                Debug.WriteLine("Подключение открыто");
                string str = $"if db_id('{_nameDatabase}') is null create database {_nameDatabase}";
                try
                {
                    SqlCommand myCommand = new SqlCommand(str, connection);
                    myCommand.ExecuteNonQuery();
                    Debug.WriteLine("Новая база создана.");
                    _connected = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }

            }
            Debug.WriteLine("Подключение закрыто...");
        }

        public async Task DeleteDatabase()
        {
            if (!IsConnected("DeleteDatabase")) return;
            if (!ContainsIntegratedSecurityString("DeleteDatabase")) return;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                try
                {
                    string str = $"if db_id('{_nameDatabase}') is not null drop database {_nameDatabase}";
                    SqlCommand myCommand = new SqlCommand(str, connection);
                    myCommand.ExecuteNonQuery();
                    Debug.WriteLine("База уничтожена.");
                    _connected = false;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }

        public async Task CreateAllTable(List<object> list_objects)
        {
            if (!IsConnected("CreateAllTable")) return;
            if (!ContainsIntegratedSecurityString("CreateAllTable")) return;

            var str2 = new StringBuilder();
            str2.Append($"USE {_nameDatabase};");

            for (int i = 0; i < list_objects.Count; i++)
            {
                Type type_ = list_objects[i].GetType();
                PropertyInfo[] list = type_.GetProperties();
                str2.Append($"IF OBJECT_ID(N'{type_.Name}s','U') IS NULL CREATE TABLE {type_.Name}s(");
                UsingTypes.Add(type_);

                foreach (var item in list)
                {
                    if (!UsingTypesString.Contains(item.PropertyType.ToString())) continue;

                    str2.Append($"{item.Name} ");
                    Regex regex = new Regex(@"Id$");
                    MatchCollection matches = regex.Matches(item.Name);

                    switch (item.PropertyType.ToString())
                    {
                        case "System.Int32":
                            if (item.Name.ToLower() == "id")
                            {
                                str2.Append($"INT PRIMARY KEY IDENTITY,");
                            }
                            else if (item.Name.ToLower() != "id" && matches.Count > 0)
                            {
                                str2.Append($"INT REFERENCES {item.Name.Replace(item.Name.Substring(item.Name.Length - 2), "")}s (id),");
                            }
                            else
                            {
                                str2.Append($"INT,");
                            }
                            break;
                        case "System.String":
                            str2.Append($"NVARCHAR(50),");
                            break;
                        case "System.DateTime":
                            str2.Append("DATETIME2 NOT NULL,");
                            break;
                        case "System.Boolean":
                            str2.Append("BIT DEFAULT 1,");
                            break;
                        default:
                            break;
                    }
                }
                str2.Append($");");
            }

            var nameProc = $"CREATE VIEW sp_GetAllEmplViews " +
                           "AS " +
                           "SELECT " +
                           "Employees.FirstName, Employees.LastName, Employees.FatherName, Employees.Birthday, Employees.Man, Employees.Employmentday, Employees.Fired, " +
                           "Employees.Dismissalday, Employees.IsMarried, Employees.HasAuto, Employees.Comment, Employees.DepId, Employees.PositionId, Deps.Name AS Dep_Name, Deps.Id AS Dep_Id, " +
                           "Positions.Name AS Pos_Name, Positions.Id AS Pos_Id, Employees.Id AS Empl_Id " +
                           "FROM " +
                           "Deps INNER JOIN " +
                           "Employees ON Deps.Id = Employees.DepId INNER JOIN " +
                           "Positions ON Employees.PositionId = Positions.Id ";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                try
                {
                    SqlCommand myCommand = new SqlCommand(str2.ToString(), connection);
                    SqlCommand myCommand2 = new SqlCommand(nameProc.ToString(), connection);
                    myCommand.ExecuteNonQuery();
                    myCommand2.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }

        public async Task PutStoreProcedure(List<object> list_objects)
        {
            if (!IsConnected("PutStoreProcedure")) return;
            if (!ContainsIntegratedSecurityString("PutStoreProcedure")) return;

            var str2 = new StringBuilder();
            var procedure_list = new List<string>();

            for (int i = 0; i < list_objects.Count; i++)
            {
                Type type_ = list_objects[i].GetType();

                var nameProc = $"CREATE PROCEDURE dbo.sp_Get{type_.Name.ToString()}s AS SELECT * FROM {type_.Name.ToString()}s";
                str2.Append(nameProc);
                procedure_list.Add(str2.ToString());
                str2.Clear();
            }

            procedure_list.Add($"CREATE PROCEDURE dbo.sp_GetAllEmployeesFromView AS SELECT * FROM sp_GetAllEmplViews");


            for (int i = 0; i < list_objects.Count; i++)
            {
                Type type_ = list_objects[i].GetType();
                var nameProc = $"CREATE PROCEDURE dbo.sp_Count{type_.Name.ToString()}s AS SELECT COUNT(*) FROM {type_.Name.ToString()}s";
                str2.Append(nameProc);
                procedure_list.Add(str2.ToString());
                str2.Clear();
            }

            for (int i = 0; i < list_objects.Count; i++)
            {
                Type type_ = list_objects[i].GetType();

                var nameProc = $"CREATE PROCEDURE dbo.sp_DeleteById{type_.Name.ToString()}s @Id INT AS DELETE {type_.Name.ToString()}s WHERE {type_.Name.ToString()}s.Id=@Id";
                str2.Append(nameProc);
                procedure_list.Add(str2.ToString());
                str2.Clear();
            }

            for (int i = 0; i < list_objects.Count; i++)
            {
                Type type_ = list_objects[i].GetType();

                var nameProc = $"CREATE PROCEDURE dbo.sp_TakeById{type_.Name.ToString()}s @Id INT AS SELECT * FROM {type_.Name.ToString()}s WHERE Id=@Id";

                str2.Append(nameProc);
                procedure_list.Add(str2.ToString());
                str2.Clear();
            }

            procedure_list.Add($"CREATE PROCEDURE dbo.sp_TakeByIdEmployeesFromViews @Id INT AS SELECT * FROM sp_GetAllEmplViews WHERE Empl_Id=@Id");

            for (int i = 0; i < 1; i++)
            {
                var nameProc = $"CREATE PROCEDURE dbo.sp_UpdateByIdEmployees" +
                               $" @Id INT, " +
                               $"@FirstName NVARCHAR(50), " +
                               $"@LastName NVARCHAR(50), " +
                               $"@FathertName NVARCHAR(50), " +
                               $"@Man BIT, " +
                               $"@IsMarried BIT, " +
                               $"@HasAuto BIT, " +
                               $"@Birthday DATETIME2, " +
                               $"@Employmentday DATETIME2, " +
                               $"@Fired BIT, " +
                               $"@Dismissalday DATETIME2, " +
                               $"@Comment NVARCHAR(50), " +
                               $"@PositionId INT, " +
                               $"@DepId INT " +
                               $"AS " +
                               $"UPDATE Employees " +
                               $"SET FirstName=@FirstName, " +
                               $"LastName=@LastName, " +
                               $"FatherName=@FathertName, " +
                               $"Birthday=@Birthday, " +
                               $"Man=@Man, " +
                               $"Employmentday=@Employmentday, " +
                               $"Fired=@Fired," +
                               $"Dismissalday=@Dismissalday," +
                               $"IsMarried=@IsMarried," +
                               $"HasAuto=@HasAuto," +
                               $"Comment=@Comment," +
                               $"DepId=@DepId," +
                               $"PositionId=@PositionId " +
                               $"WHERE Employees.Id=@Id";

                procedure_list.Add(nameProc.ToString());
            }
            
            string find_procedure = "CREATE PROCEDURE dbo.sp_FindEmploees @name NVARCHAR(50) AS " +
                                    "SELECT * FROM sp_GetAllEmplViews " +
                                    "WHERE (" +
                                    "FirstName LIKE LOWER('%'+@name+'%') OR " +
                                    "FatherName LIKE LOWER('%'+@name+'%')  OR " +
                                    "LastName LIKE LOWER('%'+@name+'%') OR " +
                                    "Dep_Name LIKE LOWER('%'+@name+'%') OR " +
                                    "Pos_Name LIKE LOWER('%'+@name+'%'))";

            #region Выборка с джойнами
            /*string find_procedure = $"CREATE PROCEDURE dbo.sp_FindEmploees @name NVARCHAR(20) AS " +
                                    $"SELECT * FROM Employees " +
                                    $"JOIN Positions ON Employees.PositionId = Positions.Id " +
                                    $"JOIN Deps ON Employees.DepId = Deps.Id " +
                                    $"WHERE ( " +
                                    $"Employees.FirstName LIKE '%'+@name+'%' OR " +
                                    $"Employees.LastName LIKE '%'+@name+'%' OR " +
                                    $"Employees.FatherName LIKE '%'+@name+'%' OR " +
                                    $"Positions.Name LIKE '%'+@name+'%' OR " +
                                    $"Deps.Name LIKE '%'+@name+'%')";*/
            #endregion
            
            procedure_list.Add(find_procedure);
            
            foreach (var proc in procedure_list)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
                    {
                        // MessageBox.Show(proc);
                        await connection.OpenAsync();
                        SqlCommand command = new SqlCommand(proc, connection);
                        // указываем, что команда представляет хранимую процедуру
                        await command.ExecuteNonQueryAsync();
                        //command.CommandType = System.Data.CommandType.StoredProcedure;
                        //var reader = command.ExecuteReader();

                    }
                }
                catch (Exception e)
                {

                    Debug.WriteLine(e.ToString());
                }
            }
        }

        public async Task Insert(object obj)
        {
            if (!IsConnected("Insert")) return;
            if (!ContainsIntegratedSecurityString("Insert")) return;
            Type type_ = obj.GetType();
            //принимаю только определенные типы
            if (!UsingTypes.Contains(type_)) return;
            var str1 = new StringBuilder();
            var str2 = new StringBuilder();
            PropertyInfo[] list = type_.GetProperties();
            str1.Append($"USE {_nameDatabase}; IF OBJECT_ID(N'{type_.Name}s','U') IS NOT NULL INSERT INTO {type_.Name}s (");
            str2.Append($" VALUES (");

            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                if (item.GetValue(obj) != null)
                {
                    switch (item.PropertyType.ToString())
                    {
                        case "System.Int32":

                            if (item.Name.ToLowerInvariant() != "id")
                            {
                                str1.Append($"{item.Name},");
                                str2.Append($"'{item.GetValue(obj)}',");
                            }
                            break;
                        case "System.String":
                            str1.Append($"{item.Name},");
                            str2.Append($"'{item.GetValue(obj)}',");
                            break;
                        case "System.DateTime":
                            str1.Append($"{item.Name},");
                            //MessageBox.Show(String.Format("{0:s}", item.GetValue(obj)));
                            str2.Append($"'{String.Format("{0:s}", item.GetValue(obj))}',");
                            break;
                        case "System.Boolean":
                            str1.Append($"{item.Name},");
                            if ((bool)item.GetValue(obj))
                            {
                                str2.Append($"1,");
                            }
                            else
                            {
                                str2.Append($"0,");
                            }
                            break;
                        default:

                            break;
                    }
                }
            }
            string com = str1.ToString().Remove(str1.ToString().Length - 1) + ")" + str2.ToString().Remove(str2.ToString().Length - 1) + ")" + " SELECT SCOPE_IDENTITY()";

            Debug.WriteLine("____"+com+"_______");
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(com.ToString(), connection);
                    int num = await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

        }

        public async Task Update(object obj, int? id)
        {
            if (!IsConnected("DBContext.Update")) return;
            if (!ContainsIntegratedSecurityString("DBContext.Update")) return;
            Type type_ = obj.GetType();
            //принимаю только определенные типы
            if (!UsingTypes.Contains(type_)) return;
            var str1 = new StringBuilder();
            var str2 = new StringBuilder();
            
            PropertyInfo[] list = type_.GetProperties();
            str1.Append($"USE {_nameDatabase}; IF OBJECT_ID(N'{type_.Name}s','U') IS NOT NULL UPDATE {type_.Name}s SET ");
            
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                if (item.GetValue(obj) != null)
                {
                    switch (item.PropertyType.ToString())
                    {
                        case "System.Int32":

                            if (item.Name.ToLowerInvariant() != "id")
                            {
                                str1.Append($"{item.Name} = {item.GetValue(obj)}, ");
                            }
                            break;
                        case "System.String":
                            str1.Append($"{item.Name} = '{item.GetValue(obj)}', ");
                            break;
                        case "System.DateTime":
                            str1.Append($"{item.Name} = CONVERT(DATETIME2, '{String.Format("{0:s}", item.GetValue(obj))}' ,102), ");
                            break;
                        case "System.Boolean":
                            var st = "0, ";
                            if ((bool)item.GetValue(obj))
                            {
                                st = "1";
                            }
                            else
                            {
                                st = "0";
                            }
                            str1.Append($"{item.Name} = {st}, ");
                            break;
                        default:
                            break;
                    }
                }

            }
            //удаляю последнюю запятую
            string com = str1.ToString();
            com = com.Remove(com.Length - 2,2) + $" WHERE Id = {id}";
            Debug.WriteLine("!__________________"+com+"____________");
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
                {
                    // MessageBox.Show(proc);
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(com.ToString(), connection);
                    // указываем, что команда представляет хранимую процедуру
                    int num = await command.ExecuteNonQueryAsync();
                    //MessageBox.Show($"Добавлено объектов: {num}");
                    //command.CommandType = System.Data.CommandType.StoredProcedure;
                    //var reader = command.ExecuteReader();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

        }

        public async Task Delete(int? id)
        {
            Employee empl = null;
            if (!IsConnected("DBContext.TakeEmployee")) return;
            if (!ContainsIntegratedSecurityString("DBContext.TakeEmployee")) return;
            //sp_TakeByIdEmployees
            string sqlExpression = $"sp_DeleteByIdEmployees";

            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);

                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    // параметр для id
                    SqlParameter nameParam = new SqlParameter
                    {
                        ParameterName = "@Id",
                        Value = id
                    };
                    command.Parameters.Add(nameParam);

                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);

                }
            }

        }

        public async Task<Employee> TakeFirstEmployee(int? id)
        {
            Employee empl = null;
            if (!IsConnected("DBContext.TakeFirstEmployee")) return null;
            if (!ContainsIntegratedSecurityString("DBContext.TakeFirstEmployee")) return null;
            //sp_TakeByIdEmployees
            string sqlExpression = $"sp_TakeByIdEmployeesFromViews";

            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);

                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    // параметр для ввода имени
                    SqlParameter nameParam = new SqlParameter
                    {
                        ParameterName = "@Id",
                        Value = id
                    };
                    // добавляем параметр
                    command.Parameters.Add(nameParam);
                  
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                               return MakeEmployee(reader);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    return null;
                }
            }

            return null;
        }

        public async Task<int> CountStorageProcedure(object obj)
        {

            if (!IsConnected("CountStorageProcedure")) return 0;
            if (!ContainsIntegratedSecurityString("CountStorageProcedure")) return 0;

            Type type_ = obj.GetType();
            string sqlExpression = $"sp_Count{type_.Name.ToString()}s";
            int count = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);
                    // указываем, что команда представляет хранимую процедуру
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    var reader = command.ExecuteScalar();

                    count = Convert.ToInt32(reader); //reader.GetString(0);

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
            return count;
        }

        public async Task<IQueryable<Employee>> GetAllEmployersStoreProc()
        {
            var list_to_return = new List<Employee>();

            if (!IsConnected("GetAllEmployersStoreProc")) return list_to_return.AsQueryable();
            if (!ContainsIntegratedSecurityString("GetAllEmployersStoreProc")) return list_to_return.AsQueryable();
            
            string sqlExpression = $"sp_GetAllEmployeesFromView";
            
            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);
                    //SqlDataAdapter adapter = new SqlDataAdapter(sqlExpression, connection);
                    //
                    //DataSet ds = new DataSet();
                    //adapter.Fill(ds);
                    //
                    //var r = ds.Tables.AsQueryable();
                    
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                list_to_return.Add(MakeEmployee(reader));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            return list_to_return.AsQueryable();
        }
        
        public async Task<IQueryable<Employee>> FindEmploees(string str)
        {
            var list_to_return = new List<Employee>();

            if (!IsConnected("FindEmploees")) return list_to_return.AsQueryable();
            if (!ContainsIntegratedSecurityString("FindEmploees")) return list_to_return.AsQueryable();


            string sqlExpression = $"sp_FindEmploees";
            Debug.WriteLine("Before GetAllEmployersStoreProc");
            //если строка пустая вывожу всех пользователей
            if (str == null || str == "") return await GetAllEmployersStoreProc();

            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);
                    
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    // параметр для id
                    SqlParameter nameParam = new SqlParameter
                    {
                        ParameterName = "@name",
                        Value = str
                    };
                    command.Parameters.Add(nameParam);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                list_to_return.Add(MakeEmployee(reader));
                                Debug.WriteLine($"After GetAllEmployersStoreProc ____{str}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("________________ "+ e + "___________________________________________");
                }
            }
            Debug.WriteLine($"After GetAllEmployersStoreProc ____{list_to_return.Count}");
            return list_to_return.AsQueryable();
        }
        
        public async Task<List<Position>> GetAllPositionsStoreProc()
        {
            var list_to_return = new List<Position>();

            if (!IsConnected("GetAllPositionsStoreProc")) return list_to_return;
            if (!ContainsIntegratedSecurityString("GetAllPositionsStoreProc")) return list_to_return;
            

            string sqlExpression = $"sp_GetPositions";

            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);

                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    using (var reader = command.ExecuteReader())
                    {
                        var column_count = reader.FieldCount;
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var pos = new Position()
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = Convert.ToString(reader["Name"]),
                                };
                                list_to_return.Add(pos);
                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            return list_to_return;
        }

        public async Task<List<Dep>> GetAllDepStoreProc()
        {
            var list_to_return = new List<Dep>();

            if (!IsConnected("GetAllDepStoreProc")) return list_to_return;
            if (!ContainsIntegratedSecurityString("GetAllDepStoreProc")) return list_to_return;
            
            string sqlExpression = $"sp_GetDeps";

            using (SqlConnection connection = new SqlConnection(_connectionString + _nameCatalog))
            {
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);

                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    using (var reader = command.ExecuteReader())
                    {
                        var column_count = reader.FieldCount;
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var dep = new Dep()
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = Convert.ToString(reader["Name"]),
                                };
                                list_to_return.Add(dep);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            return list_to_return;
        }

        private Employee MakeEmployee(SqlDataReader reader)
        {
            try
            {
                var empl = new Employee()
                {
                    Id = Convert.ToInt32(reader["Empl_Id"]),
                    FirstName = Convert.ToString(reader["FirstName"]),
                    LastName = Convert.ToString(reader["LastName"]),
                    FatherName = Convert.ToString(reader["FatherName"]),
                    Man = Convert.ToInt32(reader["Man"]) == 1 ? true : false,
                    IsMarried = Convert.ToInt32(reader["IsMarried"]) == 1 ? true : false,
                    HasAuto = Convert.ToInt32(reader["HasAuto"]) == 1 ? true : false,
                    Fired = Convert.ToInt32(reader["Fired"]) == 1 ? true : false,
                    Comment = Convert.ToString(reader["Comment"]),
                    Birthday = Convert.ToDateTime(reader["Birthday"]),
                    Employmentday = Convert.ToDateTime(reader["Employmentday"]),
                    Dismissalday = Convert.ToDateTime(reader["Dismissalday"]),
                    DepId = Convert.ToInt32(reader["DepId"]),
                    PositionId = Convert.ToInt32(reader["PositionId"]),
                    Position = new Position() { Id = Convert.ToInt32(reader["Pos_Id"]), Name = Convert.ToString(reader["Pos_Name"]) },
                    Dep = new Dep() { Id = Convert.ToInt32(reader["Dep_Id"]), Name = Convert.ToString(reader["Dep_Name"]) }

                };

                return empl;
            }
            catch (Exception e)
            {
                Debug.WriteLine("MakeEmployee:  " + e);
                return new Employee();
            }
           

            
        }

    }

}
