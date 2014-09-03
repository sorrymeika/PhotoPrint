using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace XX_PhotoPrint.Service
{
    public class Req
    {
        private IDictionary<string, string> errors;
        private NameValueCollection Form;
        private readonly HttpRequest Request;

        public Req()
        {
            this.errors = new Dictionary<string, string>();
            this.Request = HttpContext.Current.Request;
            this.Form = Request.Form;
        }

        private string getParam(string name)
        {
            if (Regex.IsMatch(name, @"^Cookie\:"))
            {
                name = Regex.Replace(name, @"^Cookie\:", "");
                var cookie = Request.Cookies[name];
                if (cookie == null)
                {
                    return null;
                }
                return cookie.Value;
            }

            string value = this.Form[name];
            if (string.IsNullOrEmpty(value))
            {
                string queryString = Request.QueryString[name];
                if (!string.IsNullOrEmpty(queryString))
                {
                    return queryString;
                }
            }
            return value;
        }

        private static readonly IDictionary<string, bool> boolConvert = new Dictionary<string, bool>()
        {
            {"on", true},
            {"off", false},
            {"", false},
            {"1", true},
            {"0", false},
            {"True", true},
            {"False", false},
            {"true", true},
            {"false", false},
        };

        public static bool Validate(string input, out string msg,
            bool emptyAble = true,
            string emptyText = null,
            string regex = null,
            string regexText = null,
            string compare = null,
            string compareText = null)
        {
            if (emptyAble == false && string.IsNullOrEmpty(input))
            {
                msg = emptyText;
            }
            else if (regex != null && !string.IsNullOrEmpty(input) && !System.Text.RegularExpressions.Regex.IsMatch(input, regex))
            {
                msg = regexText;
            }
            else if (compare != null && !string.Equals(compare, input, StringComparison.OrdinalIgnoreCase))
            {
                msg = compareText;
            }
            else
            {
                msg = null;
                return true;
            }

            return false;
        }

        public bool HasError
        {
            get { return this.errors.Count != 0; }
        }

        public void ClearErrors()
        {
            this.errors.Clear();
        }

        public IDictionary<string, string> GetErrors()
        {
            return this.errors;
        }

        public string FirstError
        {
            get
            {
                foreach (var kv in this.errors)
                {
                    return kv.Value;
                }
                return null;
            }
        }

        public string FormString(string name,
           bool emptyAble = true,
           string emptyText = null,
           string regex = null,
           string regexText = null,
           string compare = null,
           string compareText = null)
        {
            string value = Form[name];
            string msg;
            if (!Validate(value, out msg, emptyAble, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
            }
            return value;
        }

        public string String(string name,
            bool emptyAble = true,
            string emptyText = null,
            string regex = null,
            string regexText = null,
            string compare = null,
            string compareText = null)
        {
            string value = getParam(name);
            string msg;
            if (!Validate(value, out msg, emptyAble, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
            }
            return value;
        }

        public string Password(string name,
            string emptyText = null,
            string regex = null,
            string regexText = null,
            string compare = null,
            string compareText = null)
        {
            string value = getParam(name);
            string msg;
            if (!Validate(value, out msg, false, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
                return "";
            }
            return string.IsNullOrEmpty(value) ? null : System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(value, "md5");
        }

        public int Int(string name, bool emptyAble = true, string emptyText = null, string regex = null, string regexText = null, string compare = null, string compareText = null, int defaultValue = 0)
        {
            string value = getParam(name);
            string msg;
            if (!Validate(value, out msg, emptyAble, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
            }
            else
            {
                if (string.IsNullOrEmpty(value)) return defaultValue;
                try
                {
                    return int.Parse(value);
                }
                catch
                {
                    errors.Add(name, "转换为Int类型时出现错误");
                }
            }
            return defaultValue;
        }

        public decimal Decimal(string name, bool emptyAble = true, string emptyText = null, string regex = null, string regexText = null, string compare = null, string compareText = null, decimal defaultValue = 0)
        {
            string value = getParam(name);
            string msg;
            if (!Validate(value, out msg, emptyAble, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
            }
            else
            {
                if (string.IsNullOrEmpty(value)) return defaultValue;
                try
                {
                    return decimal.Parse(value);
                }
                catch
                {
                    errors.Add(name, "转换为Decimal类型时出现错误");
                }
            }
            return defaultValue;
        }

        public bool Bool(string name, bool emptyAble = true, string emptyText = null, string regex = null, string regexText = null, string compare = null, string compareText = null, bool defaultValue = false)
        {
            string value = getParam(name);
            string msg;
            if (!Validate(value, out msg, emptyAble, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
            }
            else
            {
                if (string.IsNullOrEmpty(value)) return defaultValue;
                try
                {
                    return boolConvert[value];
                }
                catch
                {
                    errors.Add(name, "转换为布尔类型时出现错误");
                }
            }
            return defaultValue;
        }

        public DateTime DateTime(string name, bool emptyAble = true, string emptyText = null, string regex = null, string regexText = null, string compare = null, string compareText = null)
        {
            string value = getParam(name);
            string msg;
            if (!Validate(value, out msg, emptyAble, emptyText, regex, regexText, compare, compareText))
            {
                errors.Add(name, msg);
            }
            else
            {
                if (string.IsNullOrEmpty(value)) return System.DateTime.MinValue;
                try
                {
                    return System.DateTime.Parse(value);
                }
                catch
                {
                    errors.Add(name, "转换为时间类型时出现错误");
                }
            }
            return System.DateTime.MinValue;
        }

        public HttpPostedFile File(string name, bool emptyAble = true, string emptyText = null, string exts = "jpg|png|bmp|gif|jpeg", string extText = "请上传图片格式文件", long? maxLength = null, string maxLengthText = null)
        {
            HttpPostedFile file = HttpContext.Current.Request.Files[name];
            if (emptyAble == false && (file == null || file.ContentLength == 0))
            {
                errors.Add(name, emptyText);
            }
            else if (maxLength != null && file.ContentLength > maxLength)
            {
                errors.Add(name, maxLengthText);
            }
            else if (exts != null && exts != "*" && (file != null && file.ContentLength != 0) && !Regex.IsMatch(file.FileName.Substring(file.FileName.LastIndexOf('.')), (@"^\.(" + exts + ")$"), RegexOptions.IgnoreCase))
            {
                errors.Add(name, extText);
            }
            return file;
        }
    }

}