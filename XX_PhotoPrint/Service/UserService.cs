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

            var user = SL.Data.SQL.QuerySingle("select UserID,UserName,Account,LatestLoginDate,Avatars,Gender,Birthday,Mobile,RealName,Address,RegionID from Users where UserID=@p0", userId);

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

        /// <summary>
        /// 发送邮件,返回true表示发送成功
        /// </summary>
        /// <param name="sender">发件人邮箱地址；发件人用户名</param>
        /// <param name="password">密码</param>
        /// <param name="receiver">接受者邮箱地址</param>
        /// <param name="host">SMTP服务器的主机名</param>
        /// <param name="sub">邮件主题行</param>
        /// <param name="body">邮件主体正文</param>
        public bool SendMain(string sender, string password, string receiver, string host, string sub, string body)
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