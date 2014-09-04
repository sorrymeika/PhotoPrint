using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SL.Util
{
    public class SessionUtil
    {
        public static void Set(string name, object value)
        {
            HttpContext.Current.Session[name] = value;
        }

        public static T Get<T>(string key)
        {
            if (Exist(key))
            {
                return (T)HttpContext.Current.Session[key];
            }
            else
            {
                return default(T);
            }
        }

        public static void Remove(string key)
        {
            try
            {
                HttpContext.Current.Session.Remove(key);
            }
            catch// (Exception exp)
            {
                //
            }
        }

        public static bool Exist(string key)
        {
            if (HttpContext.Current.Session[key] != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}