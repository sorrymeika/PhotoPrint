﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using XX_PhotoPrint.Service;
using System.IO;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Drawing;
using SL.Util;
using System.Collections.Specialized;

namespace XX_PhotoPrint.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string catalog, string handle)
        {
            this.ViewBag.RouteData = this.RouteData.Values;

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

        private Dictionary<string, object> GetOrder(int orderid)
        {
            return SQL.QueryOne("select OrderID,OrderCode,Amount,Freight,a.AddTime,a.Status,a.UserID,a.Receiver,a.Address,a.Mobile,a.Zip,b.Account from OrderInfo a join Users b on a.UserID=b.UserID where OrderID=@p0", orderid);
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


        #region 支付宝跳转
        public ActionResult alipayto(int orderId)
        {
            if (!UserService.IsLogin())
                return Redirect("/login.html");

            int uid = UserService.GetUserID();
            var orderinfo = UserService.GetOrder(orderId, uid);

            if (orderinfo == null)
                return Redirect("/");

            string ordercode = (string)orderinfo["OrderCode"];
            string username = (string)orderinfo["Account"];
            decimal orderamount = Convert.ToDecimal(orderinfo["Amount"]);
            decimal orderfreight = Convert.ToDecimal(orderinfo["Freight"]);

            ////////////////////////////////////////////请求参数////////////////////////////////////////////

            //支付类型
            string payment_type = "1";
            //必填，不能修改
            //服务器异步通知页面路径
            string notify_url = "http://" + Request.Url.Authority + "/AlipayNotifyUrl";
            //需http://格式的完整路径，不能加?id=123这类自定义参数

            //页面跳转同步通知页面路径
            string return_url = "http://" + Request.Url.Authority + "/AlipayReturnUrl";
            //需http://格式的完整路径，不能加?id=123这类自定义参数，不能写成http://localhost/

            //卖家支付宝帐户
            string seller_email = System.Configuration.ConfigurationManager.AppSettings["AlipaySellerEmail"].Trim();
            //必填

            //商户订单号
            string out_trade_no = ordercode;
            //商户网站订单系统中唯一订单号，必填

            //订单名称
            string subject = "速纺商品";
            //必填

            //付款金额
            string total_fee = orderamount.ToString("0.00");
            //必填

            //订单描述

            string body = "";
            //商品展示地址
            string show_url = "http://" + Request.Url.Authority;
            //需以http://开头的完整路径，例如：http://www.商户网址.com/myorder.html

            //防钓鱼时间戳
            string anti_phishing_key = "";
            //若要使用请调用类文件submit中的query_timestamp函数

            //客户端的IP地址
            string exter_invoke_ip = "";
            //非局域网的外网IP地址，如：221.0.0.1


            ////////////////////////////////////////////////////////////////////////////////////////////////

            //把请求参数打包成数组
            SortedDictionary<string, string> sParaTemp = new SortedDictionary<string, string>();
            sParaTemp.Add("partner", Alipay.Direct.Config.Partner);
            sParaTemp.Add("_input_charset", Alipay.Direct.Config.Input_charset.ToLower());
            sParaTemp.Add("service", "create_direct_pay_by_user");
            sParaTemp.Add("payment_type", payment_type);
            sParaTemp.Add("notify_url", notify_url);
            sParaTemp.Add("return_url", return_url);
            sParaTemp.Add("seller_email", seller_email);
            sParaTemp.Add("out_trade_no", out_trade_no);
            sParaTemp.Add("subject", subject);
            sParaTemp.Add("total_fee", total_fee);
            sParaTemp.Add("body", body);
            sParaTemp.Add("show_url", show_url);
            sParaTemp.Add("anti_phishing_key", anti_phishing_key);
            sParaTemp.Add("exter_invoke_ip", exter_invoke_ip);

            //建立请求
            string sHtmlText = Alipay.Direct.Submit.BuildRequest(sParaTemp, "get", "确认");

            ViewBag.form = new MvcHtmlString(sHtmlText);

            return View();
        }
        #endregion

        #region AlipayReturnUrl
        public ActionResult AlipayReturnUrl()
        {
            SortedDictionary<string, string> sPara = GetRequestGet();

            if (sPara.Count > 0)//判断是否有带返回参数
            {
                Alipay.Direct.Notify aliNotify = new Alipay.Direct.Notify();
                bool verifyResult = aliNotify.Verify(sPara, Request.QueryString["notify_id"], Request.QueryString["sign"]);

                if (verifyResult)//验证成功
                {
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //请在这里加上商户的业务逻辑程序代码


                    //——请根据您的业务逻辑来编写程序（以下代码仅作参考）——
                    //获取支付宝的通知返回参数，可参考技术文档中页面跳转同步通知参数列表

                    //商户订单号

                    string out_trade_no = Request.QueryString["out_trade_no"];

                    //支付宝交易号

                    string trade_no = Request.QueryString["trade_no"];

                    //交易状态
                    string trade_status = Request.QueryString["trade_status"];


                    if (Request.QueryString["trade_status"] == "TRADE_FINISHED" || Request.QueryString["trade_status"] == "TRADE_SUCCESS")
                    {
                        //判断该笔订单是否在商户网站中已经做过处理
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序
                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", out_trade_no);

                        this.UpdatePayStatus(orderId);

                        return Redirect("/Order/" + orderId);
                    }
                    else
                    {
                        return Content("trade_status=" + Request.QueryString["trade_status"]);
                    }

                    //打印页面
                    return Content("验证成功<br />");

                    //——请根据您的业务逻辑来编写程序（以上代码仅作参考）——

                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                }
                else//验证失败
                {
                    return Content("验证失败");
                }
            }
            else
            {
                return Content("无返回参数");
            }
        }


        #endregion

        #region AlipayNotifyUrl
        public ActionResult AlipayNotifyUrl()
        {
            SortedDictionary<string, string> sPara = GetRequestPost();

            if (sPara.Count > 0)//判断是否有带返回参数
            {
                Alipay.Direct.Notify aliNotify = new Alipay.Direct.Notify();
                bool verifyResult = aliNotify.Verify(sPara, Request.Form["notify_id"], Request.Form["sign"]);

                if (verifyResult)//验证成功
                {
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //请在这里加上商户的业务逻辑程序代码


                    //——请根据您的业务逻辑来编写程序（以下代码仅作参考）——
                    //获取支付宝的通知返回参数，可参考技术文档中服务器异步通知参数列表

                    //商户订单号

                    string out_trade_no = Request.Form["out_trade_no"];

                    //支付宝交易号

                    string trade_no = Request.Form["trade_no"];

                    //交易状态
                    string trade_status = Request.Form["trade_status"];


                    if (Request.Form["trade_status"] == "TRADE_FINISHED")
                    {
                        //判断该笔订单是否在商户网站中已经做过处理
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序

                        //注意：
                        //该种交易状态只在两种情况下出现
                        //1、开通了普通即时到账，买家付款成功后。
                        //2、开通了高级即时到账，从该笔交易成功时间算起，过了签约时的可退款时限（如：三个月以内可退款、一年以内可退款等）后。

                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", out_trade_no);

                        this.UpdatePayStatus(orderId);

                        return Content("success");  //请不要修改或删除
                    }
                    else if (Request.Form["trade_status"] == "TRADE_SUCCESS")
                    {
                        //判断该笔订单是否在商户网站中已经做过处理
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序

                        //注意：
                        //该种交易状态只在一种情况下出现——开通了高级即时到账，买家付款成功后。

                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", out_trade_no);

                        this.UpdatePayStatus(orderId);

                        return Content("success");  //请不要修改或删除
                    }
                    else
                    {
                    }

                    //——请根据您的业务逻辑来编写程序（以上代码仅作参考）——

                    return Content("success");  //请不要修改或删除

                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                }
                else//验证失败
                {
                    return Content("fail");
                }
            }
            else
            {
                return Content("无通知参数");
            }
        }

        public SortedDictionary<string, string> GetRequestPost()
        {
            int i = 0;
            SortedDictionary<string, string> sArray = new SortedDictionary<string, string>();
            NameValueCollection coll;
            //Load Form variables into NameValueCollection variable.
            coll = Request.Form;

            // Get names of all forms into a string array.
            String[] requestItem = coll.AllKeys;

            for (i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], Request.Form[requestItem[i]]);
            }

            return sArray;
        }
        #endregion


        #region 图片预览

        [HttpPost]
        public ActionResult ImagePreview()
        {
            RequestUtil req = new RequestUtil();

            string callback = req.String("callback");
            int width = req.Int("width", defaultValue: 640);
            int height = req.Int("height", defaultValue: 1024);

            HttpPostedFileBase pic = Request.Files.Count == 0 ? null : Request.Files[0];
            if (pic != null && pic.ContentLength != 0)
            {
                byte[] imageBuffer = ImageUtil.Compress(pic.InputStream, 40, width, height);

                string guid = System.Guid.NewGuid().ToString("N");

                CacheUtil.CreateCache("preview-" + guid, 0.1, imageBuffer);

                return Content(HtmlUtil.Result(callback, new { success = true, guid = guid, name = Request.Files.Keys[0] }));
            }
            else
            {
                return Content(HtmlUtil.Result(callback, new { success = false, msg = "您还未选择图片" }));
            }
        }

        public ActionResult ImagePreview(string guid)
        {
            guid = "preview-" + guid;

            if (CacheUtil.ExistCache(guid))
            {
                byte[] imageBuffer = CacheUtil.Get<byte[]>(guid);
                return File(imageBuffer, "image/Jpeg");
            }
            else
                return Content("图片不存在！" + guid);

        }
        #endregion

    }
}
