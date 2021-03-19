namespace Sqlconnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Dynamic;
    using System.Linq;
 

    public class SqlServerAccessManager : IDisposable
    {
        private bool _disposed = false;
        private SqlConnection _connection;

        private readonly string _connectionString;

        public SqlServerAccessManager()
        {
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["defaultSqlConnection"].ConnectionString;
        }

        public SqlServerAccessManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Connect()
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }

        public void Disconnect()
        {
            Dispose(true);
        }

        /// <summary>
        /// Executes an sql query command in the SQL Server database and returns a dynamic list of objects. 
        /// </summary>
        /// <param name="sqlCommand">Sql command.</param>
        /// <returns>Return a dynamic list of objects for the given SQL Server Query command.</returns>
        public IEnumerable<dynamic> ExecuteSQL(string sqlCommand)
        {
            var records = new List<dynamic>();

            using (SqlCommand command = new SqlCommand(sqlCommand, _connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    foreach (IDataRecord record in reader)
                    {
                        var expando = new ExpandoObject() as IDictionary<string, object>;
                        foreach (var name in names)
                            expando[name] = record[name];

                        yield return expando;
                    }

                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Executes an sql Stored Procedure in the SQL Server database and returns a dynamic list of objects. 
        /// </summary>
        /// <param name="storedProcedureName">The name of the database stored procedure.</param>
        /// <param name="o">an array of stored procedure parameters as SQLParameter object</param>
        /// <returns>Return a dynamic list of objects for the given stored procedure - parameters.</returns>
        public IEnumerable<dynamic> ExecuteProcedure(string storedProcedureName, params SqlParameter[] o)
        {
            var records = new List<dynamic>();

            using (SqlCommand command = new SqlCommand(storedProcedureName, _connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                foreach (var param in o)
                {
                    SqlParameter sqlParam;
                    sqlParam = command.Parameters.Add(param.ParameterName, param.SqlDbType);
                    sqlParam.Value = param.Value;
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    foreach (IDataRecord record in reader)
                    {
                        var expando = new ExpandoObject() as IDictionary<string, object>;
                        foreach (var name in names)
                            expando[name] = record[name];

                        yield return expando;
                    }

                    reader.Close();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        _connection.Dispose();
                    }
                }
                // Release unmanaged resources.
                // Set large fields to null.                
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlServerAccessManager() // the finalizer
        {
            Dispose(false);
        }
    }
}
