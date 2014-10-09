using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SL.Data
{
    public static class SQL
    {
        public static int Execute(string sql, params object[] parameters)
        {
            using (Database db = Database.Open())
            {
                return db.Execute(sql, parameters);
            }
        }

        public static int Execute(string sql, out int identity, params object[] parameters)
        {
            using (Database db = Database.Open())
            {
                return db.Execute(sql, out identity, parameters);
            }
        }

        public static T QueryValue<T>(string sql, params object[] parameters)
        {
            using (Database db = Database.Open())
            {
                return db.QueryValue<T>(sql, parameters);
            }
        }

        public static bool Exists(string sql, params object[] parameters)
        {
            using (Database db = Database.Open())
            {
                return db.Exists(sql, parameters);
            }
        }

        public static IList<dynamic> Query(string sql, params object[] parameters)
        {
            using (Database db = Database.Open())
            {
                return db.Query(sql, parameters);
            }
        }

        public static IList<dynamic> QueryPage(string pks,
            string select,
            string from,
            string where,
            int page,
            int pageSize,
            IEnumerable<object> parameters,
            out int total,
            IDictionary<string, bool> sorts = null)
        {
            using (Database db = Database.Open())
            {
                return db.QueryPage(pks.Split(','), select, from, where, page, pageSize, parameters, out total, sorts);
            }
        }

        public static IList<dynamic> QueryPage(string[] pks,
            string select,
            string from,
            string where,
            int page,
            int pageSize,
            IEnumerable<object> parameters,
            out int total,
            IDictionary<string, bool> sorts = null)
        {
            using (Database db = Database.Open())
            {
                return db.QueryPage(pks, select, from, where, page, pageSize, parameters, out total, sorts);
            }
        }

        public static dynamic QuerySingle(string sql, params object[] parameters)
        {
            using (Database db = Database.Open())
            {
                return db.QuerySingle(sql, parameters);
            }
        }
    }

}