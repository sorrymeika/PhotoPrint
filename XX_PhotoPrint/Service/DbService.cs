using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Text;

namespace XX_PhotoPrint.Service
{
    public class DbService : IDisposable
    {
        protected readonly DbConnection Connection;
        private DbTransaction trans;
        protected readonly IDbCommand cmd;

        public DbService()
            : this(System.Configuration.ConfigurationManager.ConnectionStrings["XX_PhotoPrint"].ConnectionString)
        {
        }

        public DbService(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
            cmd = Connection.CreateCommand();
        }

        protected void Open()
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();
        }

        protected void Close()
        {
            if (Connection.State == ConnectionState.Open)
                Connection.Close();
        }

        public void BeginTrans()
        {
            this.Open();
            trans = Connection.BeginTransaction();
            cmd.Transaction = trans;
        }

        public void Commit()
        {
            trans.Commit();
        }

        public void Rollback()
        {
            trans.Rollback();
        }

        public int Execute(string sql, params object[] parameters)
        {
            this.Open();

            cmd.Parameters.Clear();
            cmd.CommandText = sql;
            if (parameters != null && parameters.Length != 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    cmd.Parameters.Add(new SqlParameter("@p" + i, parameters[i] == null ? DBNull.Value : parameters[i]));
                }
            }
            return cmd.ExecuteNonQuery();
        }

        public int Execute(string sql, out int identity, params object[] parameters)
        {
            this.Open();

            cmd.Parameters.Clear();
            cmd.CommandText = sql + " select @identity=@@IDENTITY";
            SqlParameter identityParam = new SqlParameter();
            identityParam.ParameterName = "@identity";
            identityParam.DbType = DbType.Int32;
            identityParam.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(identityParam);
            if (parameters != null && parameters.Length != 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    cmd.Parameters.Add(new SqlParameter("@p" + i, parameters[i] == null ? DBNull.Value : parameters[i]));
                }
            }
            int result = cmd.ExecuteNonQuery();
            identity = (int)identityParam.Value;
            return result;
        }

        public T QueryScalar<T>(string sql, params object[] parameters)
        {
            this.Open();

            cmd.Parameters.Clear();
            cmd.CommandText = sql;
            if (parameters != null && parameters.Length != 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    cmd.Parameters.Add(new SqlParameter("@p" + i, parameters[i] == null ? DBNull.Value : parameters[i]));
                }
            }
            object result = cmd.ExecuteScalar();
            if (result == null)
                return default(T);
            if (typeof(T) == typeof(bool))
                result = Convert.ToBoolean(result);
            return (T)result;
        }

        public bool Exists(string sql, params object[] parameters)
        {
            sql = "if exists (" + sql + ") select 1 else select 0";
            return QueryScalar<bool>(sql, parameters);
        }

        public IList<Dictionary<string, object>> Query(string sql, params object[] parameters)
        {
            this.Open();
            cmd.Parameters.Clear();
            cmd.CommandText = sql;
            if (parameters != null && parameters.Length != 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    cmd.Parameters.Add(new SqlParameter("@p" + i, parameters[i] == null ? DBNull.Value : parameters[i]));
                }
            }
            using (DbDataReader dr = (DbDataReader)cmd.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    Dictionary<string, object> item;
                    string columnName;
                    Type type;
                    while (dr.Read())
                    {
                        item = new Dictionary<string, object>();
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            columnName = dr.GetName(i);
                            type = dr.GetFieldType(i);
                            if (type == typeof(DateTime))
                            {
                                item.Add(columnName, dr[i] == DBNull.Value ? null : dr.GetDateTime(i).ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                            else if (dr[i] == DBNull.Value)
                            {
                                if (type == typeof(bool))
                                    item.Add(columnName, false);
                                else if (type.IsValueType)
                                    item.Add(columnName, 0);
                                else
                                    item.Add(columnName, null);
                            }
                            else
                            {
                                item.Add(columnName, dr[i]);
                            }
                        }
                        list.Add(item);
                    }

                    return list;
                }

            }
            return null;
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

        public IList<Dictionary<string, object>> QueryPage(string[] pks,
            string columns,
            string table,
            string where,
            int page,
            int pageSize,
            object[] parameters,
            out int total,
            IDictionary<string, bool> sorts = null)
        {
            if (string.IsNullOrEmpty(where))
                where = "1=1";

            IDictionary<string, string> conditions = new Dictionary<string, string>() { 
                {"table", table},
                {"where", where}
            };

            total = this.QueryScalar<int>(formatString("select count(1) from {table} where {where}", conditions), parameters);

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

        public Dictionary<string, object> QueryOne(string sql, params object[] parameters)
        {
            this.Open();
            cmd.Parameters.Clear();
            cmd.CommandText = sql;
            if (parameters != null && parameters.Length != 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    cmd.Parameters.Add(new SqlParameter("@p" + i, parameters[i] == null ? DBNull.Value : parameters[i]));
                }
            }
            using (DbDataReader dr = (DbDataReader)cmd.ExecuteReader())
            {
                if (dr.HasRows && dr.Read())
                {
                    Dictionary<string, object> item;
                    string columnName;
                    Type type;
                    item = new Dictionary<string, object>();
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        columnName = dr.GetName(i);
                        type = dr.GetFieldType(i);
                        if (type == typeof(DateTime))
                        {
                            item.Add(columnName, dr[i] == DBNull.Value ? null : dr.GetDateTime(i).ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        else if (dr[i] == DBNull.Value)
                        {
                            if (type == typeof(bool))
                                item.Add(columnName, false);
                            else if (type.IsValueType)
                                item.Add(columnName, 0);
                            else
                                item.Add(columnName, null);
                        }
                        else
                        {
                            item.Add(columnName, dr[i]);
                        }
                    }
                    return item;
                }
            }
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isDispose)
        {
            this.Connection.Dispose();
            this.cmd.Dispose();
            if (trans != null)
            {
                trans.Dispose();
                trans = null;
            }
        }
    }

}