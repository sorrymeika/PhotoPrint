using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XX_PhotoPrint.Service
{
    public static class ServiceExtentions
    {
        public static T Get<T>(this IDictionary<string, object> data, string key)
        {
            try
            {
                return (T)data[key];
            }
            catch
            {
                return default(T);
            }
        }
    }
}