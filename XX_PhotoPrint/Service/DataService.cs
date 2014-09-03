using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.IO;

namespace XX_PhotoPrint.Service
{
    /// <summary>
    /// 类型T必须有ID属性
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataService<T>
    {
        private readonly string path;
        private readonly JavaScriptSerializer serializer;
        private readonly IList<T> data;
        private readonly string name;

        private static readonly IDictionary<string, DataService<T>> dataServiceList = new Dictionary<string, DataService<T>>();
        public static DataService<T> Get(string name)
        {
            if (dataServiceList.ContainsKey(name))
            {
                return dataServiceList[name];
            }
            else
            {
                var service = new DataService<T>(name);
                dataServiceList.Add(name, service);
                return service;
            }
        }

        public DataService(string name)
        {
            this.name = name;
            this.path = Path.Combine(HttpContext.Current.Server.MapPath("~/json"), name + ".json");
            this.serializer = new JavaScriptSerializer();
            if (File.Exists(this.path))
            {
                string data;
                using (StreamReader sr = File.OpenText(path))
                {
                    data = sr.ReadToEnd();
                }
                this.data = serializer.Deserialize<IList<T>>(data);
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
            File.WriteAllText(path, serializer.Serialize(data));
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