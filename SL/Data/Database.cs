using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Text;
using System.Configuration;

namespace SL.Data
{
    public class Database : IDisposable
    {
        protected readonly DbConnection _connection;
        private DbTransaction _transaction;
        protected readonly DbCommand _command;

        private static readonly Dictionary<string, string> lastInsertedIdStmts = new Dictionary<string, string>();
        private readonly string providerName;

        static Database()
        {
            lastInsertedIdStmts.Add("System.Data.SqlClient", "select @@IDENTITY;");
            lastInsertedIdStmts.Add("Mono.Data.Sqlite", "select last_insert_rowid ();");
            lastInsertedIdStmts.Add("MySql.Data", "SELECT LAST_INSERT_ID();");
            lastInsertedIdStmts.Add("Npgsql", "SELECT lastval();");
        }

        public static Database Open()
        {
            return Open("XX_PhotoPrint");
        }

        public static Database Open(string name)
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[name];
            if (settings == null)
            {
                throw new ArgumentException("name", string.Format("Database with name {0} doesn't exist", name));
            }
            return OpenConnectionString(settings.ConnectionString, settings.ProviderName);
        }

        public static Database OpenConnectionString(string connectionString)
        {
            return OpenConnectionString(connectionString, "System.Data.SqlClient");
        }

        public static Database OpenConnectionString(string connectionString, string providerName)
        {
            return new Database(connectionString, providerName);
        }

        private Database(string connectionString, string providerName)
        {
            this.providerName = providerName;
            _connection = DbProviderFactories.GetFactory(providerName).CreateConnection();
            _connection.ConnectionString = connectionString;
            _command = _connection.CreateCommand();
        }

        protected void EnsureConnectionOpen()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        private void PrepareCommand(string sql, object[] args)
        {
            _command.Parameters.Clear();
            _command.CommandText = sql;
            if (args != null && args.Length != 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    _command.Parameters.Add(new SqlParameter("@p" + i, args[i] ?? DBNull.Value));
                }
            }
        }

        protected void Close()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }

        public void BeginTrans()
        {
            this.EnsureConnectionOpen();
            _transaction = _connection.BeginTransaction();
            _command.Transaction = _transaction;
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public int Execute(string sql, params object[] parameters)
        {
            EnsureConnectionOpen();
            PrepareCommand(sql, parameters);
            return _command.ExecuteNonQuery();
        }

        public int Execute(string sql, out int identity, params object[] parameters)
        {
            EnsureConnectionOpen();
            PrepareCommand(sql + " select @identity=@@IDENTITY", parameters);

            SqlParameter identityParam = new SqlParameter();
            identityParam.ParameterName = "@identity";
            identityParam.DbType = DbType.Int32;
            identityParam.Direction = ParameterDirection.Output;
            _command.Parameters.Add(identityParam);

            int result = _command.ExecuteNonQuery();
            identity = (int)identityParam.Value;
            return result;
        }

        public T GetLastInsertId<T>()
        {
            string str;
            if (!lastInsertedIdStmts.TryGetValue(this.providerName, out str))
            {
                throw new NotSupportedException("This operation is not available for your database");
            }
            return this.QueryValue<T>(str, new object[0]);
        }

        public T QueryValue<T>(string sql, params object[] parameters)
        {
            EnsureConnectionOpen();
            PrepareCommand(sql, parameters);

            object result = _command.ExecuteScalar();
            if (result == null)
                return default(T);
            if (typeof(T) == typeof(bool))
                result = Convert.ToBoolean(result);
            return (T)result;
        }

        public bool Exists(string sql, params object[] parameters)
        {
            sql = "if exists (" + sql + ") select 1 else select 0";
            return QueryValue<bool>(sql, parameters);
        }


        private IList<dynamic> QueryInternal(string commandText, object[] parameters, bool unique)
        {
            EnsureConnectionOpen();
            PrepareCommand(commandText, parameters);

            try
            {
                using (DbDataReader reader = _command.ExecuteReader())
                {
                    if (reader.HasRows && reader.Read())
                    {
                        string[] strArray = new string[reader.FieldCount];
                        List<dynamic> list = new List<dynamic>();
                        do
                        {
                            Dictionary<string, object> item = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (strArray[i] == null)
                                {
                                    strArray[i] = reader.GetName(i);
                                }
                                item[strArray[i]] = reader[i];
                            }
                            list.Add(new DynamicRecord(item));
                        }
                        while (!unique && reader.Read());
                        return list;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception(commandText, ex);
            }
            return null;
        }

        public IList<dynamic> Query(string commandText, params object[] parameters)
        {
            return QueryInternal(commandText, parameters, false);
        }

        private string formatString(string oriString, IDictionary<string, string> format)
        {
            foreach (KeyValuePair<string, string> kp in format)
            {
                oriString = oriString.Replace("{" + kp.Key + "}", kp.Value);
            }
            return oriString;
        }

        private static readonly IDictionary<bool, string> ascOrDesc = new Dictionary<bool, string>() { 
            { true, "asc" },
            { false, "desc" }
        };

        public IList<dynamic> QueryPage(string pks,
            string select,
            string from,
            string where,
            int page,
            int pageSize,
            IEnumerable<object> parameters,
            out int total,
            IDictionary<string, bool> sorts = null)
        {
            return this.QueryPage(pks.Split(','), select, from, where, page, pageSize, parameters, out total, sorts);
        }


        public IList<dynamic> QueryPage(string[] pks,
            string columns,
            string table,
            string where,
            int page,
            int pageSize,
            IEnumerable<object> iparameters,
            out int total,
            IDictionary<string, bool> sorts = null)
        {
            if (string.IsNullOrEmpty(where))
                where = "1=1";

            IDictionary<string, string> conditions = new Dictionary<string, string>() { 
                {"table", table},
                {"where", where}
            };

            object[] parameters = iparameters.ToArray();

            total = this.QueryValue<int>(formatString("select count(1) from {table} where {where}", conditions), parameters);

            if (total != 0)
            {
                StringBuilder sbSortFields = new StringBuilder();
                StringBuilder sbSortAs = new StringBuilder();
                StringBuilder sbSortAsc = new StringBuilder(" order by ");
                StringBuilder sbSortDesc = new StringBuilder(" order by ");
                StringBuilder sbSortOrg = new StringBuilder(" order by ");
                if (sorts == null || sorts.Count == 0)
                {
                    sorts = new Dictionary<string, bool>();
                    foreach (var pk in pks)
                    {
                        sorts.Add(pk, true);
                    }
                }

                int i = 0;
                foreach (var sort in sorts)
                {
                    if (i != 0)
                    {
                        sbSortAs.Append(",");
                        sbSortFields.Append(",");
                        sbSortAsc.Append(",");
                        sbSortDesc.Append(",");
                        sbSortOrg.Append(",");
                    }
                    sbSortFields.Append("_sort_field")
                        .Append(i.ToString());

                    sbSortAs.Append(sort.Key)
                        .Append(" as _sort_field")
                        .Append(i.ToString());

                    sbSortAsc.Append("_sort_field")
                        .Append(i.ToString())
                        .Append(" ")
                        .Append(ascOrDesc[sort.Value]);

                    sbSortDesc.Append("_sort_field")
                        .Append(i.ToString())
                        .Append(" ")
                        .Append(ascOrDesc[!sort.Value]);

                    sbSortOrg.Append(sort.Key)
                        .Append(" ")
                        .Append(ascOrDesc[sort.Value]);

                    i++;
                }


                conditions.Add("sortOrg", sbSortOrg.ToString());
                conditions.Add("columns", columns);

                string sql;
                if (page == 1)
                {
                    conditions.Add("top", pageSize.ToString());
                    sql = formatString("select top {top} {columns} from {table} where {where} {sortOrg}", conditions);
                }
                else
                {
                    int max = pageSize * page;
                    int top = max <= total ? pageSize : (total - max + pageSize);
                    if (top <= 0) return null;

                    StringBuilder sbPK = new StringBuilder();
                    StringBuilder sbPKFields = new StringBuilder();
                    StringBuilder sbJoin = new StringBuilder();
                    string tmp;
                    for (i = 0; i < pks.Length; i++)
                    {
                        if (i != 0)
                        {
                            sbPK.Append(",");
                            sbPKFields.Append(",");
                            sbPKFields.Append(" and");
                        }
                        tmp = "_pk_field" + i.ToString();
                        sbPK.Append(pks[i])
                            .Append(" as ")
                            .Append(tmp);

                        sbPKFields.Append(tmp);

                        sbJoin.Append(" a.")
                            .Append(tmp)
                            .Append("=b.")
                            .Append(tmp);
                    }

                    conditions.Add("top", top.ToString());
                    conditions.Add("max", max.ToString());
                    conditions.Add("pkAs", sbPK.ToString());
                    conditions.Add("sortAs", sbSortAs.ToString());
                    conditions.Add("pkFields", sbPKFields.ToString());
                    conditions.Add("sortFields", sbSortFields.ToString());
                    conditions.Add("sortAsc", sbSortAsc.ToString());
                    conditions.Add("sortDesc", sbSortDesc.ToString());
                    conditions.Add("join", sbJoin.ToString());
                    conditions.Add("needs", Regex.Replace(columns, @"(^|(?<=,))\s*([^\.,]+\.|[^,]+?\s+as\s+)", ""));

                    sql = formatString("select {needs} from (select top {top} {pkFields},{sortFields} from (select top {max} {pkAs},{sortAs} from {table} where {where} {sortAsc}) a {sortDesc}) a inner join (select {pkAs},{columns} from {table}) b on {join} {sortAsc}", conditions);
                }

                return this.Query(sql, parameters);
            }
            else
            {
                return null;
            }
        }

        public dynamic QuerySingle(string commandText, params object[] parameters)
        {
            IList<dynamic> list = QueryInternal(commandText, parameters, true);
            return (list == null || list.Count == 0) ? null : list[0];
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
                this._command.Dispose();
                this._connection.Close();
                this._connection.Dispose();
            }
        }
    }
}