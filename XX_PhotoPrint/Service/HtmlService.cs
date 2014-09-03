using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;

namespace XX_PhotoPrint.Service
{
    public class HtmlService
    {
        static readonly System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();

        public static string SubmitResult(string func, dynamic obj)
        {
            return "<script>window.parent." + func + "(" + js.Serialize(obj) + ");</script>";
        }

        public static string CallbackResult(string callback, dynamic obj)
        {
            if (string.IsNullOrEmpty(callback))
            {
                return js.Serialize(obj);
            }
            else
            {
                return "<script>window.parent." + callback + "(" + js.Serialize(obj) + ");</script>";
            }
        }
    }
}