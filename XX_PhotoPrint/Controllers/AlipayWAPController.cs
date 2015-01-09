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
    public class AlipayWAPController : Controller
    {
        protected void UpdatePayStatus(int orderid)
        {
            SQL.Execute("update OrderInfo set Status=1 where OrderID=@p0", orderid);
        }

        protected Dictionary<string, object> GetOrder(int orderid)
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
            var merchant_url = url + "order/" + orderid;
            var call_back_url = url + "pay/alipaycallback/" + orderid;
            var notify_url = url + "pay/alipaynotify/" + orderid;

            string ordercode = (string)order["OrderCode"];
            string username = (string)order["Account"];
            decimal orderamount = Convert.ToDecimal(order["Amount"]);
            decimal orderfreight = Convert.ToDecimal(order["Freight"]);

            return RedirectToAlipay(Config.Alipay_Seller_account_name, ordercode, "速纺", (orderamount + orderfreight).ToString("0.00"), notify_url, call_back_url, merchant_url);
        }


        #region 支付宝（手机WAP）
        /// <summary>
        /// 跳转至支付宝支付
        /// </summary>
        /// <param name="seller_email">卖家支付宝帐户(必填)</param>
        /// <param name="out_trade_no">商户订单号(商户网站订单系统中唯一订单号，必填)</param>
        /// <param name="subject">订单名称(必填)</param>
        /// <param name="total_fee">付款金额(必填)</param>
        /// <param name="notify_url">服务器异步通知页面路径</param>
        /// <param name="call_back_url">页面跳转同步通知页面路径</param>
        /// <param name="merchant_url">操作中断返回地址</param>
        /// <returns></returns>
        public ActionResult RedirectToAlipay(string seller_email, string out_trade_no, string subject, string total_fee, string notify_url, string call_back_url, string merchant_url)
        {
            Alipay.WAP.Config.Partner = Config.Alipay_Partner;
            Alipay.WAP.Config.Private_key = Config.Alipay_PrivateKey;
            Alipay.WAP.Config.Public_key = Config.Alipaypublick;
            Alipay.WAP.Config.Sign_type = "MD5";
            Alipay.WAP.Config.Key = Config.AlipayKey;

            //支付宝网关地址
            string GATEWAY_NEW = "http://wappaygw.alipay.com/service/rest.htm?";

            ////////////////////////////////////////////调用授权接口alipay.wap.trade.create.direct获取授权码token////////////////////////////////////////////

            //返回格式
            string format = "xml";
            //必填，不需要修改

            //返回格式
            string v = "2.0";
            //必填，不需要修改

            //请求号
            string req_id = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            //必填，须保证每次请求都是唯一

            //请求业务参数详细
            string req_dataToken = "<direct_trade_create_req><notify_url>" + notify_url + "</notify_url><call_back_url>" + call_back_url + "</call_back_url><seller_account_name>" + seller_email + "</seller_account_name><out_trade_no>" + out_trade_no + "</out_trade_no><subject>" + subject + "</subject><total_fee>" + total_fee + "</total_fee><merchant_url>" + merchant_url + "</merchant_url></direct_trade_create_req>";
            //必填

            //把请求参数打包成数组
            Dictionary<string, string> sParaTempToken = new Dictionary<string, string>();
            sParaTempToken.Add("partner", Alipay.WAP.Config.Partner);
            sParaTempToken.Add("_input_charset", Alipay.WAP.Config.Input_charset.ToLower());
            sParaTempToken.Add("sec_id", Alipay.WAP.Config.Sign_type.ToUpper());
            sParaTempToken.Add("service", "alipay.wap.trade.create.direct");
            sParaTempToken.Add("format", format);
            sParaTempToken.Add("v", v);
            sParaTempToken.Add("req_id", req_id);
            sParaTempToken.Add("req_data", req_dataToken);

            //建立请求
            string sHtmlTextToken = Alipay.WAP.Submit.BuildRequest(GATEWAY_NEW, sParaTempToken);
            //URLDECODE返回的信息
            System.Text.Encoding code = System.Text.Encoding.GetEncoding(Alipay.WAP.Config.Input_charset);
            sHtmlTextToken = HttpUtility.UrlDecode(sHtmlTextToken, code);

            //解析远程模拟提交后返回的信息
            Dictionary<string, string> dicHtmlTextToken = Alipay.WAP.Submit.ParseResponse(sHtmlTextToken);

            //获取token
            string request_token = dicHtmlTextToken["request_token"];

            ////////////////////////////////////////////根据授权码token调用交易接口alipay.wap.auth.authAndExecute////////////////////////////////////////////


            //业务详细
            string req_data = "<auth_and_execute_req><request_token>" + request_token + "</request_token></auth_and_execute_req>";
            //必填

            //把请求参数打包成数组
            Dictionary<string, string> sParaTemp = new Dictionary<string, string>();
            sParaTemp.Add("partner", Alipay.WAP.Config.Partner);
            sParaTemp.Add("_input_charset", Alipay.WAP.Config.Input_charset.ToLower());
            sParaTemp.Add("sec_id", Alipay.WAP.Config.Sign_type.ToUpper());
            sParaTemp.Add("service", "alipay.wap.auth.authAndExecute");
            sParaTemp.Add("format", format);
            sParaTemp.Add("v", v);
            sParaTemp.Add("req_data", req_data);

            //建立请求
            string sHtmlText = Alipay.WAP.Submit.BuildRequest(GATEWAY_NEW, sParaTemp, "get", "确认");
            return Content(sHtmlText);
        }


        public ActionResult Callback(int orderid)
        {
            Dictionary<string, string> sPara = GetRequestGet();

            if (sPara.Count > 0)//判断是否有带返回参数
            {
                Alipay.WAP.Notify aliNotify = new Alipay.WAP.Notify();
                bool verifyResult = aliNotify.VerifyReturn(sPara, Request.QueryString["sign"]);

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
                    string result = Request.QueryString["result"];


                    //判断是否在商户网站中已经做过了这次通知返回的处理
                    //如果没有做过处理，那么执行商户的业务程序
                    //如果有做过处理，那么不执行商户的业务程序
                    this.UpdatePayStatus(orderid);

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

        public ActionResult Notify(int orderid)
        {
            Dictionary<string, string> sPara = GetRequestPost();

            if (sPara.Count > 0)//判断是否有带返回参数
            {
                Alipay.WAP.Notify aliNotify = new Alipay.WAP.Notify();
                bool verifyResult = aliNotify.VerifyNotify(sPara, Request.Form["sign"]);

                if (verifyResult)//验证成功
                {
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //请在这里加上商户的业务逻辑程序代码


                    //——请根据您的业务逻辑来编写程序（以下代码仅作参考）——
                    //获取支付宝的通知返回参数，可参考技术文档中服务器异步通知参数列表

                    //解密（如果是RSA签名需要解密，如果是MD5签名则下面一行清注释掉）
                    sPara = aliNotify.Decrypt(sPara);

                    //XML解析notify_data数据
                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(sPara["notify_data"]);
                        //商户订单号
                        string out_trade_no = xmlDoc.SelectSingleNode("/notify/out_trade_no").InnerText;
                        //支付宝交易号
                        string trade_no = xmlDoc.SelectSingleNode("/notify/trade_no").InnerText;
                        //交易状态
                        string trade_status = xmlDoc.SelectSingleNode("/notify/trade_status").InnerText;

                        if (trade_status == "TRADE_FINISHED")
                        {
                            //判断该笔订单是否在商户网站中已经做过处理
                            //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                            //如果有做过处理，不执行商户的业务程序

                            //注意：
                            //该种交易状态只在两种情况下出现
                            //1、开通了普通即时到账，买家付款成功后。
                            //2、开通了高级即时到账，从该笔交易成功时间算起，过了签约时的可退款时限（如：三个月以内可退款、一年以内可退款等）后。
                            this.UpdatePayStatus(orderid);

                            return Content("success");  //请不要修改或删除
                        }
                        else if (trade_status == "TRADE_SUCCESS")
                        {
                            //判断该笔订单是否在商户网站中已经做过处理
                            //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                            //如果有做过处理，不执行商户的业务程序

                            //注意：
                            //该种交易状态只在一种情况下出现——开通了高级即时到账，买家付款成功后。

                            this.UpdatePayStatus(orderid);

                            return Content("success");  //请不要修改或删除
                        }
                        else
                        {
                            return Content(trade_status);
                        }

                    }
                    catch (Exception exc)
                    {
                        return Content(exc.ToString());
                    }



                    //——请根据您的业务逻辑来编写程序（以上代码仅作参考）——

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

        /// <summary>
        /// 获取支付宝POST过来通知消息，并以“参数名=参数值”的形式组成数组
        /// </summary>
        /// <returns>request回来的信息组成的数组</returns>
        public Dictionary<string, string> GetRequestPost()
        {
            int i = 0;
            Dictionary<string, string> sArray = new Dictionary<string, string>();
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

        /// <summary>
        /// 获取支付宝GET过来通知消息，并以“参数名=参数值”的形式组成数组
        /// </summary>
        /// <returns>request回来的信息组成的数组</returns>
        public Dictionary<string, string> GetRequestGet()
        {
            int i = 0;
            Dictionary<string, string> sArray = new Dictionary<string, string>();
            NameValueCollection coll;
            //Load Form variables into NameValueCollection variable.
            coll = Request.QueryString;

            // Get names of all forms into a string array.
            String[] requestItem = coll.AllKeys;

            for (i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], Request.QueryString[requestItem[i]]);
            }

            return sArray;
        }
        #endregion

    }
}
