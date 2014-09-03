using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Alipay.Class;
using XX_PhotoPrint.Service;
using System.IO;
using System.Globalization;
using System.Web.Script.Serialization;

namespace XX_PhotoPrint.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string catalog, string handle)
        {
            return View("~/Views/" + catalog + "/" + handle + ".cshtml");
        }

        public ActionResult JsonAction(string catalog, string handle)
        {
            return View("~/Views/Json/" + catalog + "/" + handle + ".cshtml");
        }

        public ActionResult Manage(string catalog, string handle = null)
        {
            string admin = SessionService.Get<string>("Admin");
            if (string.IsNullOrEmpty(admin) && !"login".Equals(catalog, StringComparison.OrdinalIgnoreCase))
            {
                if (Request.AcceptTypes.Contains("application/json"))
                {
                    return Json(new { success = false, msg = "请先登录" });
                }
                else
                {
                    return Redirect(Url.Content("~/Manage/Login"));
                }
            }
            return View("~/Views/Manage/" + catalog + (string.IsNullOrEmpty(handle) ? "" : ("/" + handle)) + ".cshtml");
        }

        #region 图片上传
        public ActionResult Upload(string dir = null)
        {
            HttpPostedFileBase imgFile = Request.Files["imgFile"];
            if (imgFile == null)
            {
                return showError("请选择文件。");
            }

            int maxSize = 5000000;

            if (imgFile.InputStream == null || imgFile.InputStream.Length > maxSize)
            {
                return showError("上传文件大小超过限制。");
            }

            //定义允许上传的文件扩展名
            Dictionary<string, string> extTable = new Dictionary<string, string>();
            extTable.Add("image", "gif,jpg,jpeg,png,bmp");
            extTable.Add("flash", "swf,flv");
            extTable.Add("media", "swf,flv,mp3,wav,wma,wmv,mid,avi,mpg,asf,rm,rmvb");
            extTable.Add("file", "doc,docx,xls,xlsx,ppt,htm,html,txt,zip,rar,gz,bz2");
            String dirName = dir;
            if (String.IsNullOrEmpty(dirName))
            {
                dirName = "image";
            }
            if (!extTable.ContainsKey(dirName))
            {
                return showError("目录名不正确。");
            }

            string dirDay = DateTime.Today.ToString("yy-MM-dd");
            String dirPath = Server.MapPath("~/upload") + "\\" + dirName + "\\" + dirDay;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            String fileName = imgFile.FileName;
            String fileExt = Path.GetExtension(fileName).ToLower();

            if (String.IsNullOrEmpty(fileExt) || Array.IndexOf(((String)extTable[dirName]).Split(','), fileExt.Substring(1).ToLower()) == -1)
            {
                return showError("上传文件扩展名是不允许的扩展名。\n只允许" + ((String)extTable[dirName]) + "格式。");
            }

            String newFileName = DateTime.Now.ToString("yyyyMMddHHmmss_ffff", DateTimeFormatInfo.InvariantInfo) + fileExt;
            String filePath = Path.Combine(dirPath, newFileName);

            imgFile.SaveAs(filePath);

            String fileUrl = "http://" + Request.Url.Authority + "/upload/" + dirName + "/" + dirDay + "/" + newFileName;

            return Content(new JavaScriptSerializer().Serialize(new { error = 0, url = fileUrl }));
        }

        private ActionResult showError(string msg)
        {
            return Content(new JavaScriptSerializer().Serialize(new { error = 1, message = msg }));
        }
        #endregion

        #region 验证码
        public ActionResult CheckCode()
        {
            string checkCode;
            byte[] imageBuffer = ImageService.CreateImage(out checkCode);
            Session["CheckCode"] = checkCode;

            return File(imageBuffer, "image/Jpeg");
        }
        #endregion

        #region 订单支付（支付宝）
        private Dictionary<string, object> GetOrder(int orderid)
        {
            return SQL.QueryOne("select OrderID,OrderCode,Amount,Freight,a.AddTime,a.Status,a.UserID,a.Receiver,a.Address,a.Mobile,a.Zip,b.Account from OrderInfo a join Users b on a.UserID=b.UserID where OrderID=@p0", orderid);
        }

        public ActionResult Pay(int orderid)
        {
            var req = new Req();
            string account = req.String("Account", false, "未输入账号");
            string auth = req.String("Auth", false, "未输入授权");
            if (req.HasError)
            {
                return Content(req.FirstError);
            }

            auth = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(auth, "md5");
            string sAuth = UserService.GetAuth(account);
            if (!auth.Equals(sAuth, StringComparison.OrdinalIgnoreCase))
            {
                return Content("授权错误");
            }

            int uid = UserService.GetUserID(account);

            var order = GetOrder(orderid);

            if (order == null)
            {
                return Content("订单不存在");
            }

            int orderStatus = (int)order["Status"];
            if (orderStatus != 0)
            {
                return Content("订单状态有误");
            }

            string url = "http://" + Request.Url.Authority + "/";
            AlipayConfig.Merchant_url = url + "pay/success/?orderid=" + orderid;
            AlipayConfig.Call_back_url = url + "pay/alipaycallback/?orderid" + orderid;
            AlipayConfig.Notify_url = url + "pay/alipaynotify/?orderid" + orderid;
            AlipayConfig.PrivateKey = Config.Alipay_PrivateKey;
            AlipayConfig.Partner = Config.Alipay_Partner;
            AlipayConfig.Seller_account_name = Config.Alipay_Seller_account_name;

            string ordercode = (string)order["OrderCode"];
            string username = (string)order["Account"];
            decimal orderamount = Convert.ToDecimal(order["Amount"]);
            decimal orderfreight = Convert.ToDecimal(order["Freight"]);

            //初始化Service
            Alipay.Class.Service ali = new Alipay.Class.Service();
            //创建交易接口
            string token = ali.alipay_wap_trade_create_direct(
               AlipayConfig.Req_url, AlipayConfig.Subject, ordercode, (orderamount + orderfreight).ToString("0.00"), AlipayConfig.Seller_account_name, AlipayConfig.Notify_url,
               username, AlipayConfig.Merchant_url, AlipayConfig.Call_back_url, AlipayConfig.Service_Create, AlipayConfig.Sec_id, AlipayConfig.Partner, AlipayConfig.Req_id, AlipayConfig.Format, AlipayConfig.V, AlipayConfig.Req_url, AlipayConfig.PrivateKey, AlipayConfig.Input_charset_UTF8);

            //构造，重定向URL
            url = ali.alipay_Wap_Auth_AuthAndExecute(AlipayConfig.Req_url, AlipayConfig.Sec_id, AlipayConfig.Partner, AlipayConfig.Call_back_url, AlipayConfig.Format, AlipayConfig.V, AlipayConfig.Service_Auth, token, AlipayConfig.Req_url, AlipayConfig.PrivateKey, AlipayConfig.Input_charset_UTF8);
            //跳转收银台支付页面
            return Redirect(url);
        }


        public ActionResult AlipayPayCallback(int orderid)
        {
            var order = GetOrder(orderid);
            if (order == null)
            {
                return Content("fail");
            }

            AlipayConfig.PrivateKey = Config.Alipay_PrivateKey;
            AlipayConfig.Alipaypublick = Config.Alipaypublick;

            //获取签名
            string sign = Request["sign"];
            //获取所有参数
            SortedDictionary<string, string> sArrary = GetRequestGet();

            bool isVerify = Function.Verify(sArrary, sign, AlipayConfig.Alipaypublick, AlipayConfig.Input_charset_UTF8);
            if (!isVerify)
            {
                //验签出错，可能被别人篡改数据
                return Content("fail");
            }
            string result = Request["result"];
            if (!result.Equals("success"))
            {
                return Content("fail");
            }
            else //交易成功，请填写自己的业务代码
            {
                this.UpdatePayStatus(orderid);
                return Content("success");

                ///////////////////////////////处理数据/////////////////////////////////
                // 用户这里可以写自己的商业逻辑
                // 例如：修改数据库订单状态
                // 仅进行演示如何调取
                // 参数对照请详细查阅开发文档
                // 里面有详细说明
                // 参数获取 直接用Request["参数名"] GET方式获取即可
                ////////////////////////////////////////////////////////////////////////////
            }
        }

        private void UpdatePayStatus(int orderid)
        {
            SQL.Execute("update OrderInfo set Status=1 where OrderID=@p0", orderid);
        }

        /// <summary>
        /// 获取所有Callback参数
        /// </summary>
        /// <returns>SortedDictionary格式参数</returns>
        public SortedDictionary<string, string> GetRequestGet()
        {
            SortedDictionary<string, string> sArray = new SortedDictionary<string, string>();
            string query = Request.Url.Query.Replace("?", "");
            if (!string.IsNullOrEmpty(query))
            {
                string[] coll = query.Split('&');

                string[] temp = { };

                for (int i = 0; i < coll.Length; i++)
                {
                    temp = coll[i].Split('=');

                    sArray.Add(temp[0], temp[1]);
                }
            }
            return sArray;
        }

        public ActionResult AlipayPaySuccess(int orderid)
        {
            return Content("支付成功！");
        }

        public ActionResult AlipayPayNotify(int orderid)
        {
            AlipayConfig.PrivateKey = Config.Alipay_PrivateKey;
            AlipayConfig.Alipaypublick = Config.Alipaypublick;

            //前台页面别忘记加这句指令，否则会报特殊字符的异常 ValidateRequest="false"

            //获取加密的notify_data数据
            string notify_data = Request.Form["notify_data"];

            //通过商户私钥进行解密
            notify_data = Function.Decrypt(notify_data, AlipayConfig.PrivateKey, AlipayConfig.Input_charset_UTF8);
            //获取签名
            string sign = Request.Form["sign"];

            //创建待签名数组，注意Notify这里数组不需要进行排序，请保持以下顺序
            Dictionary<string, string> sArrary = new Dictionary<string, string>();

            //组装验签数组
            sArrary.Add("service", Request.Form["service"]);
            sArrary.Add("v", Request.Form["v"]);
            sArrary.Add("sec_id", Request.Form["sec_id"]);
            sArrary.Add("notify_data", notify_data);

            //把数组所有元素，按照“参数=参数值”的模式用“&”字符拼接成字符串
            string content = Function.CreateLinkString(sArrary);

            //验证签名
            bool vailSign = Function.Verify(content, sign, AlipayConfig.Alipaypublick, AlipayConfig.Input_charset_UTF8);
            if (!vailSign)
            {
                return Content("fail");
            }

            //获取交易状态
            string trade_status = Function.GetStrForXmlDoc(notify_data, "notify/trade_status");

            if (!trade_status.Equals("TRADE_FINISHED"))
            {
                return Content("fail");
            }
            else
            {
                this.UpdatePayStatus(orderid);
                return Content("success");

                ///////////////////////////////处理数据/////////////////////////////////
                // 用户这里可以写自己的商业逻辑
                // 例如：修改数据库订单状态
                // 以下数据仅仅进行演示如何调取
                // 参数对照请详细查阅开发文档
                // 里面有详细说明
                //string subject = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/subject");
                //string buyer_email = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/buyer_email");
                //string out_trade_no = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/out_trade_no");
                //string total_fee = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/total_fee");
                //string seller_email = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/seller_email");
                //string price = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/price");
                //string notify_id = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/notify_id");
                //string gmt_payment = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/gmt_payment");
                //string gmt_close = Alipay.Class.Function.GetStrForXmlDoc(notify_data, "notify/gmt_close");
                ////////////////////////////////////////////////////////////////////////////

            }
        }
        #endregion
    }
}
