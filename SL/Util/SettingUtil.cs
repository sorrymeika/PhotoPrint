using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Helpers;

namespace SL.Util
{

    public class SettingUtil
    {
    }

    public class SettingUtil<T>
    {
        private readonly string path;
        private readonly IList<T> data;
        private readonly string name;

        private static readonly IDictionary<string, SettingUtil<T>> dataServiceList = new Dictionary<string, SettingUtil<T>>();
        public static SettingUtil<T> Get(string name)
        {
            if (dataServiceList.ContainsKey(name))
            {
                return dataServiceList[name];
            }
            else
            {
                var service = new SettingUtil<T>(name);
                dataServiceList.Add(name, service);
                return service;
            }
        }

        public SettingUtil(string name)
        {
            this.name = name;
            this.path = Path.Combine(HttpContext.Current.Server.MapPath("~/json"), name + ".json");
            if (File.Exists(this.path))
            {
                string data;
                using (StreamReader sr = File.OpenText(path))
                {
                    data = sr.ReadToEnd();
                }
                this.data = Json.Decode<IList<T>>(data);
            }
            if (this.data == null)
            {
                this.data = new List<T>();
            }
        }

        public IList<T> Get()
        {
            return data;
        }

        public IEnumerable<T> Get(Func<T, bool> where)
        {
            return data.Where(where);
        }

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            return data.FirstOrDefault(predicate);
        }

        public T FirstOrDefault()
        {
            return data.FirstOrDefault();
        }

        public void Add(T item)
        {
            var type = item.GetType();
            var prop = type.GetProperty("ID");
            if (prop != null && prop.CanRead && prop.CanWrite)
            {
                prop.SetValue(item, data.Count == 0 ? 1 : (data.Max(a => (int)prop.GetValue(a, null)) + 1), null);
            }
            data.Add(item);
            save();
        }

        public void Save()
        {
            save();
        }

        private void save()
        {
            File.WriteAllText(path, Json.Encode(data));
        }

        public bool Delete(T item)
        {
            var flag = data.Remove(item);
            save();
            return flag;
        }

        public bool Delete(Func<T, bool> condition)
        {
            var flag = false;
            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (condition(data[i]))
                {
                    flag = true;
                    data.RemoveAt(i);
                }
            }
            save();
            return flag;
        }
    }


}