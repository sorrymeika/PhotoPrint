using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Drawing;
using SL.Util;
using System.Collections.Specialized;
using System.Xml;
using XX_PhotoPrint.Service;

namespace XX_PhotoPrint.Controllers
{
    public class AlipayDoubleController : Controller
    {
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
            string price = orderamount.ToString("0.00");
            //必填

            //商品数量
            string quantity = "1";
            //必填，建议默认为1，不改变值，把一次交易看成是一次下订单而非购买一件商品
            //物流费用
            string logistics_fee = orderfreight.ToString("0.00");
            //必填，即运费
            //物流类型
            string logistics_type = "EXPRESS";
            //必填，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
            //物流支付方式
            string logistics_payment = "BUYER_PAY";
            //必填，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）
            //订单描述

            string body = "";
            //商品展示地址
            string show_url = "http://" + Request.Url.Authority;
            //需以http://开头的完整路径，如：http://www.商户网站.com/myorder.html

            //收货人姓名
            string receive_name = (string)orderinfo["Receiver"];
            //如：张三

            //收货人地址
            string receive_address = (string)orderinfo["Address"];
            //如：XX省XXX市XXX区XXX路XXX小区XXX栋XXX单元XXX号

            //收货人邮编
            string receive_zip = (string)orderinfo["Zip"];
            //如：123456

            //收货人电话号码
            string receive_phone = (string)orderinfo["Phone"];
            //如：0571-88158090

            //收货人手机号码
            string receive_mobile = (string)orderinfo["Mobile"];
            //如：13312341234


            ////////////////////////////////////////////////////////////////////////////////////////////////

            //把请求参数打包成数组
            SortedDictionary<string, string> sParaTemp = new SortedDictionary<string, string>();
            sParaTemp.Add("partner", Alipay.Double.Config.Partner);
            sParaTemp.Add("_input_charset", Alipay.Double.Config.Input_charset.ToLower());
            sParaTemp.Add("service", "trade_create_by_buyer");
            sParaTemp.Add("payment_type", payment_type);
            sParaTemp.Add("notify_url", notify_url);
            sParaTemp.Add("return_url", return_url);
            sParaTemp.Add("seller_email", seller_email);
            sParaTemp.Add("out_trade_no", out_trade_no);
            sParaTemp.Add("subject", subject);
            sParaTemp.Add("price", price);
            sParaTemp.Add("quantity", quantity);
            sParaTemp.Add("logistics_fee", logistics_fee);
            sParaTemp.Add("logistics_type", logistics_type);
            sParaTemp.Add("logistics_payment", logistics_payment);
            sParaTemp.Add("body", body);
            sParaTemp.Add("show_url", show_url);
            sParaTemp.Add("receive_name", receive_name);
            sParaTemp.Add("receive_address", receive_address);
            sParaTemp.Add("receive_zip", receive_zip);
            sParaTemp.Add("receive_phone", receive_phone);
            sParaTemp.Add("receive_mobile", receive_mobile);

            //建立请求
            string sHtmlText = Alipay.Double.Submit.BuildRequest(sParaTemp, "get", "确认");

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
                Alipay.Double.Notify aliNotify = new Alipay.Double.Notify();
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
                Alipay.Double.Notify aliNotify = new Alipay.Double.Notify();
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

    }
}
