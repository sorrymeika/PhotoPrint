using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using SL.Util;

namespace XX_PhotoPrint.Service
{
    public class UserService
    {
        public static bool IsLogin()
        {
            return SessionUtil.Exist("USERINFO");
        }

        public static IDictionary<string, object> GetUser()
        {
            return SessionUtil.Get<IDictionary<string, object>>("USERINFO");
        }

        public static dynamic GetUserFullInfo()
        {
            var userId = GetUserID();

            if (userId == 0) return null;

            var user = SL.Data.SQL.QuerySingle("select UserID,UserName,Account,LatestLoginDate,Avatars,Gender,Birthday,Mobile,RealName,Address,a.RegionID,b.CityID,CityName,RegionName,c.ProvID,c.ProvName from Users a left join Region d on a.RegionID=d.RegionID left join City b on d.CityID=b.CityID left join Province c on b.ProvID=c.ProvID where UserID=@p0", userId);

            if (user != null)
            {
                user.Avatars = "http://" + HttpContext.Current.Request.Url.Authority + "/Content/" + user.Avatars;
            }

            return user;
        }

        public static string GetUserName()
        {
            return IsLogin() ? GetUser()["UserName"] as String : null;
        }

        public static string GetUserAuth()
        {
            return IsLogin() ? GetUser()["Auth"] as String : null;
        }

        public static int GetUserID()
        {
            return IsLogin() ? (int)GetUser()["UserID"] : 0;
        }

        public static int GetUserID(string account)
        {
            IDictionary<string, int> data;
            if (CacheService.ExistCache("UserID"))
            {
                data = CacheService.Get<IDictionary<string, int>>("UserID");
                if (data.ContainsKey(account))
                {
                    return data[account];
                }
            }
            else
            {
                data = new Dictionary<string, int>();
                CacheService.Set("UserID", data);
            }
            int userId = SQL.QueryScalar<int>("select UserID from Users where Account=@p0", account);
            data[account] = userId;
            return userId;
        }

        public static string GetAuth(string account)
        {
            if (CacheService.ExistCache("Auth"))
            {
                var data = CacheService.Get<IDictionary<string, string>>("Auth");
                if (data.ContainsKey(account))
                {
                    return data[account];
                }
            }
            string auth = SQL.QueryScalar<string>("select Auth from Users where Account=@p0", account);
            SetAuth(account, auth);
            return auth;
        }

        public static void SetAuth(string account, string auth)
        {
            IDictionary<string, string> data;
            if (CacheService.ExistCache("Auth"))
            {
                data = CacheService.Get<IDictionary<string, string>>("Auth");
                data[account] = auth;
            }
            else
            {
                data = new Dictionary<string, string>
                {
                    { account, auth }
                };
                CacheService.Set("Auth", data);
            }
        }

        public static IList<dynamic> GetCoupons(int uid)
        {
            return SL.Data.SQL.Query("select a.CouponID,c.Code,a.CategoryID,Title,Memo,CouponDateFrom,CouponDate as CouponDateTo,Price,CategoryName,c.UseTimes from Coupon a join CouponCate b on a.CategoryID=b.CategoryID join CouponCode c on a.CouponID=c.CouponID where CouponDate>=@p0 and c.UserID=@p1", DateTime.Now, uid);
        }

        public static IList<dynamic> GetAddress(int uid)
        {
            return SL.Data.SQL.Query("select AddressID,UserID,Receiver,a.CityID,CityName,a.RegionID,RegionName,c.ProvID,c.ProvName,Zip,Address,TelPhone,Mobile,IsCommonUse from UserAddress a left join City b on a.CityID=b.CityID left join Province c on b.ProvID=c.ProvID left join Region d on a.RegionID=d.RegionID where UserID=@p0", uid);
        }

        public static IList<dynamic> GetOrders(int uid, int[] status, int page, int pageSize, out int total)
        {
            List<object> parameters = new List<object>();
            string where = "a.UserID=@p0";
            parameters.Add(uid);

            if (status != null)
            {
                where += " and a.Status in (" + string.Join(",", status) + ")";
            }

            return GetOrders(page, pageSize, out total, where, parameters);
        }

        public static IList<dynamic> GetOrders(int uid, int status, int page, int pageSize, out int total)
        {
            List<object> parameters = new List<object>();
            string where = "a.UserID=@p0";
            parameters.Add(uid);

            if (status != null)
            {
                where += " and a.Status=@p1";
                parameters.Add(status);
            }

            return GetOrders(page, pageSize, out total, where, parameters);
        }

        public static IList<dynamic> GetOrders(int page, int pageSize, out int total, string sqlWhere, IEnumerable<object> sqlParams)
        {
            using (SL.Data.Database db = SL.Data.Database.Open())
            {
                string where = "1=1";
                List<object> parameters = new List<object>();

                if (!string.IsNullOrEmpty(sqlWhere))
                {
                    where += " and " + sqlWhere;
                    parameters.AddRange(sqlParams);
                }

                var list = db.QueryPage("OrderID", "OrderID,OrderCode,Amount,Freight,a.Discount,a.AddTime,a.Status,a.UserID,a.PaymentID,a.Receiver,a.Address,a.Mobile,a.Phone,a.Zip,b.Account,a.CityID,a.RegionID,c.CityName,e.RegionName,d.ProvName",
                    "OrderInfo a join Users b on a.UserID=b.UserID left join City c on a.CityID=c.CityID left join Province d on c.ProvID=d.ProvID left join Region e on a.RegionID=e.RegionID",
                    where,
                    page,
                    pageSize,
                    parameters.ToArray(),
                    out total);

                if (list != null)
                {

                    foreach (var data in list)
                    {
                        var detailList = db.Query("select c.OrderID,c.OrderDetailID,c.UserWorkID,c.Qty,a.ProductID,b.ProductName,a.Picture,b.Price from OrderDetail c join UserWork a on c.UserWorkID=a.UserWorkID join Product b on a.ProductID=b.ProductID where OrderID=@p0", (int)data.OrderID);

                        string url = "http://" + HttpContext.Current.Request.Url.Authority + "/";
                        detailList.All(a =>
                        {
                            a["Picture"] = url + a["Picture"];
                            a["Styles"] = db.Query("select a.StyleID,StyleName,Rect,b.ColorID,c.ColorName,b.SizeID,SizeName from Style a left join UserCustomization b on a.StyleID=b.StyleID left join Color c on b.ColorID=c.ColorID left join ProductSize d on b.SizeID=d.SizeID where UserWorkID=@p0 order by a.StyleID", a["UserWorkID"]);
                            return true;
                        });
                        data["Details"] = detailList;

                        data["PaymentName"] = PaymentService.Payments.First(a => (int)a["PaymentID"] == (int)data["PaymentID"])["PaymentName"];
                        data["Total"] = (decimal)data["Freight"] + (decimal)data["Amount"];
                    }
                }
                return list;
            }
        }

        public static dynamic GetOrder(string ordercode, int uid)
        {
            return GetOrder(SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", ordercode), uid);
        }

        public static dynamic GetOrder(int orderid, int uid)
        {
            using (SL.Data.Database db = SL.Data.Database.Open())
            {
                var data = db.QuerySingle("select OrderID,OrderCode,Inv,Amount,Freight,a.Discount,a.AddTime,a.Status,a.UserID,a.PaymentID,a.Receiver,a.Address,a.Mobile,a.Phone,a.Zip,b.Account,a.CityID,a.RegionID,c.CityName,e.RegionName,d.ProvName from OrderInfo a join Users b on a.UserID=b.UserID left join City c on a.CityID=c.CityID left join Province d on c.ProvID=d.ProvID left join Region e on a.RegionID=e.RegionID where OrderID=@p0 and a.UserID=@p1", orderid, uid);

                var detailList = db.Query("select c.OrderID,c.OrderDetailID,c.UserWorkID,c.Qty,a.ProductID,b.ProductName,a.Picture,b.Price from OrderDetail c join UserWork a on c.UserWorkID=a.UserWorkID join Product b on a.ProductID=b.ProductID where OrderID=@p0", orderid);

                string url = "http://" + HttpContext.Current.Request.Url.Authority + "/";
                detailList.All(a =>
                {
                    a["Picture"] = url + a["Picture"];
                    a["Styles"] = db.Query("select a.StyleID,StyleName,Rect,b.ColorID,c.ColorName,b.SizeID,SizeName from Style a left join UserCustomization b on a.StyleID=b.StyleID left join Color c on b.ColorID=c.ColorID left join ProductSize d on b.SizeID=d.SizeID where UserWorkID=@p0 order by a.StyleID", a["UserWorkID"]);
                    return true;
                });
                data["Details"] = detailList;

                data["PaymentName"] = PaymentService.Payments.First(a => (int)a["PaymentID"] == (int)data["PaymentID"])["PaymentName"];
                data["Total"] = (decimal)data["Freight"] + (decimal)data["Amount"];

                return data;
            }
        }

        private static readonly string sn = "SDK-WSS-010-05920";//序列号
        private static readonly string password = "3Af2342-";//密码
        private static readonly string subcode = "";//扩展码
        private static readonly string stime = "";//定时时间
        public static string SendSms(string mobiles, string msg)
        {
            string pwd = Md5.MD5(sn + password);
            string msgResult = Functions.Function.Mt(sn, pwd, mobiles, msg + "【速纺】", subcode, stime, "");

            return msgResult;
        }

        /// <summary>
        /// 发送邮件,返回true表示发送成功
        /// </summary>
        /// <param name="sender">发件人邮箱地址；发件人用户名</param>
        /// <param name="password">密码</param>
        /// <param name="receiver">接受者邮箱地址</param>
        /// <param name="host">SMTP服务器的主机名</param>
        /// <param name="sub">邮件主题行</param>
        /// <param name="body">邮件主体正文</param>
        public static bool SendMain(string sender, string password, string receiver, string host, string sub, string body)
        {
            System.Net.Mail.SmtpClient client = new SmtpClient();
            client.Host = host;
            client.UseDefaultCredentials = false;

            client.Credentials = new System.Net.NetworkCredential(sender, password);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            try
            {
                System.Net.Mail.MailMessage message = new MailMessage(sender, receiver);
                message.Subject = sub;
                message.Body = body;
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.IsBodyHtml = true;
                client.Send(message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                client.Dispose();
            }
        }


    }
}