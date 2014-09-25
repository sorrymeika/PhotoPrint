using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Caching;

namespace SL.Util
{
    /// <summary>
    /// 类型T必须有ID属性
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheUtil
    {
        private static void onRemove(string strIdentify, object userInfo, CacheItemRemovedReason reason)
        {

        }
        //建立回调委托的一个实例
        CacheItemRemovedCallback callBack = new CacheItemRemovedCallback(onRemove);

        /// <summary>
        /// 创建缓存对象（默认24小时缓存）
        /// </summary>
        /// <param name="name">键名</param>
        /// <param name="value">键值</param>
        public static void CreateCache(string name, object value)
        {
            CreateCache(name, 24, value);
        }

        /// <summary>
        /// 创建缓存对象(指定缓存小时)
        /// </summary>
        /// <param name="name">键名</param>
        /// <param name="hours">缓存时间(小时)</param>
        /// <param name="value">键值</param>
        public static void CreateCache(string name, double hours, object value)
        {
            //建立回调委托的一个实例
            CacheItemRemovedCallback callBack = new CacheItemRemovedCallback(onRemove);

            //以Identify为标志，将userInfo存入Cache
            HttpRuntime.Cache.Insert(name, value, null,
                 System.DateTime.Now.AddHours(hours),//当前指定为24小时,7*24=168
                 System.Web.Caching.Cache.NoSlidingExpiration,
                 System.Web.Caching.CacheItemPriority.Default,
                 callBack);
        }

        public static T Get<T>(string key)
        {
            if (ExistCache(key))
            {
                return (T)HttpRuntime.Cache[key];
            }
            else
            {
                return default(T);
            }
        }

        public static void Set(string key, object data)
        {
            HttpRuntime.Cache[key] = data;
        }

        public static void Remove(string key)
        {
            try
            {
                HttpContext.Current.Cache.Remove(key);
            }
            catch// (Exception exp)
            {
                //
            }
        }

        public static bool ExistCache(string key)
        {
            if (HttpRuntime.Cache[key] != null)
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