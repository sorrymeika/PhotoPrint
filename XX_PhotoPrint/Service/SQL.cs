using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XX_PhotoPrint.Service
{
    public static class SQL
    {
        public static int Execute(string sql, params object[] parameters)
        {
            using (DbService db = new DbService())
            {
                return db.Execute(sql, parameters);
            }
        }

        public static int Execute(string sql, out int identity, params object[] parameters)
        {
            using (DbService db = new DbService())
            {
                return db.Execute(sql, out identity, parameters);
            }
        }

        public static T QueryScalar<T>(string sql, params object[] parameters)
        {
            using (DbService db = new DbService())
            {
                return db.QueryScalar<T>(sql, parameters);
            }
        }

        public static bool Exists(string sql, params object[] parameters)
        {
            using (DbService db = new DbService())
            {
                return db.Exists(sql, parameters);
            }
        }

        public static IList<Dictionary<string, object>> Query(string sql, params object[] parameters)
        {
            using (DbService db = new DbService())
            {
                return db.Query(sql, parameters);
            }
        }

        public static IList<Dictionary<string, object>> QueryPage(string[] pks,
            string columns,
            string table,
            string where,
            int page,
            int pageSize,
            object[] parameters,
            out int total,
            IDictionary<string, bool> sorts = null)
        {
            using (DbService db = new DbService())
            {
                return db.QueryPage(pks, columns, table, where, page, pageSize, parameters, out  total, sorts);
            }
        }

        public static Dictionary<string, object> QueryOne(string sql, params object[] parameters)
        {
            using (DbService db = new DbService())
            {
                return db.QueryOne(sql, parameters);
            }
        }
    }

}