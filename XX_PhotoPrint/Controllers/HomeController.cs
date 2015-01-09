using System;
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
using Alipay.Class;

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

        #region 订单支付（支付宝WAP）
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
            AlipayConfig.Merchant_url = url + "pay/success/" + orderid;
            AlipayConfig.Call_back_url = url + "pay/alipaycallback/" + orderid;
            AlipayConfig.Notify_url = url + "pay/alipaynotify/" + orderid;
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


        #region 支付宝跳转
        public ActionResult alipayto(int orderId)
        {
            if (!UserService.IsLogin())
                return Redirect("/login.html");

            int uid = UserService.GetUserID();
            var orderinfo = UserService.GetOrder(orderId, uid);

            if (orderinfo == null)
                return Redirect("/");

            ///////////////////////以下参数是需要设置的相关配置参数，设置后不会更改的///////////////////////////
            AlipayClass.AlipayConfig con = new AlipayClass.AlipayConfig();
            string partner = con.Partner;
            string key = con.Key;
            string seller_email = con.Seller_email;
            string input_charset = con.Input_charset;
            string notify_url = con.Notify_url;
            string return_url = con.Return_url;
            string show_url = con.Show_url;
            string sign_type = con.Sign_type;

            ////////////////////////////////////////////////////////////////////////////////////////////////////

            ///////////////////////以下参数是需要通过下单时的订单数据传入进来获得////////////////////////////////
            //必填参数
            string out_trade_no = orderinfo.OrderCode;  //请与贵网站订单系统中的唯一订单号匹配
            string subject = "速纺商品";                      //订单名称，显示在支付宝收银台里的“商品名称”里，显示在支付宝的交易管理的“商品名称”的列表里。
            string body = "速纺商品";                          //订单描述、订单详细、订单备注，显示在支付宝收银台里的“商品描述”里
            string price = orderinfo.Amount.ToString("0.00");                        //订单总金额，显示在支付宝收银台里的“商品单价”里

            string logistics_fee = orderinfo.Freight.ToString("0.00");                  				//物流费用，即运费。
            string logistics_type = "EXPRESS";				                //物流类型，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
            string logistics_payment = "BUYER_PAY";            			//物流支付方式，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）

            string quantity = "1";              							//商品数量，建议默认为1，不改变值，把一次交易看成是一次下订单而非购买一件商品。

            //扩展参数——买家收货信息（推荐作为必填）
            //该功能作用在于买家已经在商户网站的下单流程中填过一次收货信息，而不需要买家在支付宝的付款流程中再次填写收货信息。
            //若要使用该功能，请至少保证receive_name、receive_address有值
            //收货信息格式请严格按照姓名、地址、邮编、电话、手机的格式填写
            string receive_name = orderinfo.Receiver;			                    //收货人姓名，如：张三
            string receive_address = orderinfo.Address;			                //收货人地址，如：XX省XXX市XXX区XXX路XXX小区XXX栋XXX单元XXX号
            string receive_zip = orderinfo.Zip;                  			    //收货人邮编，如：123456
            string receive_phone = orderinfo.Phone;                		    //收货人电话号码，如：0571-81234567
            string receive_mobile = orderinfo.Mobile;               		    //收货人手机号码，如：13312341234

            //扩展参数——第二组物流方式
            //物流方式是三个为一组成组出现。若要使用，三个参数都需要填上数据；若不使用，三个参数都需要为空
            //有了第一组物流方式，才能有第二组物流方式，且不能与第一个物流方式中的物流类型相同，
            //即logistics_type="EXPRESS"，那么logistics_type_1就必须在剩下的两个值（POST、EMS）中选择
            string logistics_fee_1 = "";                					//物流费用，即运费。
            string logistics_type_1 = "";               					//物流类型，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
            string logistics_payment_1 = "";           					    //物流支付方式，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）

            //扩展参数——第三组物流方式
            //物流方式是三个为一组成组出现。若要使用，三个参数都需要填上数据；若不使用，三个参数都需要为空
            //有了第一组物流方式和第二组物流方式，才能有第三组物流方式，且不能与第一组物流方式和第二组物流方式中的物流类型相同，
            //即logistics_type="EXPRESS"、logistics_type_1="EMS"，那么logistics_type_2就只能选择"POST"
            string logistics_fee_2 = "";                					//物流费用，即运费。
            string logistics_type_2 = "";               					//物流类型，三个值可选：EXPRESS（快递）、POST（平邮）、EMS（EMS）
            string logistics_payment_2 = "";            					//物流支付方式，两个值可选：SELLER_PAY（卖家承担运费）、BUYER_PAY（买家承担运费）

            //扩展功能参数——其他
            string buyer_email = "";                    					//默认买家支付宝账号
            string discount = "";                       					//折扣，是具体的金额，而不是百分比。若要使用打折，请使用负数，并保证小数点最多两位数

            /////////////////////////////////////////////////////////////////////////////////////////////////////

            //构造请求函数，无需修改
            AlipayClass.AlipayService aliService = new AlipayClass.AlipayService(
                partner,
                seller_email,
                return_url,
                notify_url,
                show_url,
                out_trade_no,
                subject,
                body,
                price,
                logistics_fee,
                logistics_type,
                logistics_payment,
                quantity,
                receive_name,
                receive_address,
                receive_zip,
                receive_phone,
                receive_mobile,
                logistics_fee_1,
                logistics_type_1,
                logistics_payment_1,
                logistics_fee_2,
                logistics_type_2,
                logistics_payment_2,
                buyer_email,
                discount,
                key,
                input_charset,
                sign_type);
            string sHtmlText = aliService.Build_Form();

            ViewBag.form = new MvcHtmlString(sHtmlText);

            return View();
        }
        #endregion

        #region AlipayReturnUrl
        public ActionResult AlipayReturnUrl()
        {
            SortedDictionary<string, string> sArrary = GetRequestGet();
            ///////////////////////以下参数是需要设置的相关配置参数，设置后不会更改的//////////////////////
            AlipayClass.AlipayConfig con = new AlipayClass.AlipayConfig();
            string partner = con.Partner;
            string key = con.Key;
            string input_charset = con.Input_charset;
            string sign_type = con.Sign_type;
            string transport = con.Transport;
            //////////////////////////////////////////////////////////////////////////////////////////////

            if (sArrary.Count > 0)//判断是否有带返回参数
            {
                AlipayClass.AlipayNotify aliNotify = new AlipayClass.AlipayNotify(sArrary, Request.QueryString["notify_id"], partner, key, input_charset, sign_type, transport);
                string responseTxt = aliNotify.ResponseTxt; //获取远程服务器ATN结果，验证是否是支付宝服务器发来的请求
                string sign = Request.QueryString["sign"];  //获取支付宝反馈回来的sign结果
                string mysign = aliNotify.Mysign;           //获取通知返回后计算后（验证）的签名结果

                //写日志记录（若要调试，请取消下面两行注释）
                //string sWord = "responseTxt=" + responseTxt + "\n return_url_log:sign=" + Request.QueryString["sign"] + "&mysign=" + mysign + "\n return回来的参数：" + aliNotify.PreSignStr;
                //AlipayFunction.log_result(Server.MapPath("log/" + DateTime.Now.ToString().Replace(":", "")) + ".txt",sWord);

                //判断responsetTxt是否为ture，生成的签名结果mysign与获得的签名结果sign是否一致
                //responsetTxt的结果不是true，与服务器设置问题、合作身份者ID、notify_id一分钟失效有关
                //mysign与sign不等，与安全校验码、请求时的参数格式（如：带自定义参数等）、编码格式有关
                if (responseTxt == "true" && sign == mysign)//验证成功
                {
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //请在这里加上商户的业务逻辑程序代码

                    //——请根据您的业务逻辑来编写程序（以下代码仅作参考）——
                    //获取支付宝的通知返回参数，可参考技术文档中页面跳转同步通知参数列表
                    string trade_no = Request.QueryString["trade_no"];              //支付宝交易号
                    string order_no = Request.QueryString["out_trade_no"];	        //获取订单号
                    string total_fee = Request.QueryString["price"];	            //获取总金额
                    string subject = Request.QueryString["subject"];                //商品名称、订单名称
                    string body = Request.QueryString["body"];                      //商品描述、订单备注、描述
                    string buyer_email = Request.QueryString["buyer_email"];        //买家支付宝账号
                    string receive_name = Request.QueryString["receive_name"];      //收货人姓名
                    string receive_address = Request.QueryString["receive_address"];//收货人地址
                    string receive_zip = Request.QueryString["receive_zip"];        //收货人邮编
                    string receive_phone = Request.QueryString["receive_phone"];    //收货人电话
                    string receive_mobile = Request.QueryString["receive_mobile"];  //收货人手机
                    string trade_status = Request.QueryString["trade_status"];      //交易状态

                    if (Request.QueryString["trade_status"] == "WAIT_SELLER_SEND_GOODS")
                    {
                        //判断该笔订单是否在商户网站中已经做过处理（可参考“集成教程”中“3.4返回数据处理”）
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序

                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", order_no);

                        this.UpdatePayStatus(orderId);

                        return Redirect("/Order/" + orderId);
                    }
                    else
                    {
                        return Content("trade_status=" + Request.QueryString["trade_status"]);
                    }
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
            SortedDictionary<string, string> sArrary = GetRequestPost();
            ///////////////////////以下参数是需要设置的相关配置参数，设置后不会更改的//////////////////////
            AlipayClass.AlipayConfig con = new AlipayClass.AlipayConfig();
            string partner = con.Partner;
            string key = con.Key;
            string input_charset = con.Input_charset;
            string sign_type = con.Sign_type;
            string transport = con.Transport;
            //////////////////////////////////////////////////////////////////////////////////////////////

            if (sArrary.Count > 0)//判断是否有带返回参数
            {
                AlipayClass.AlipayNotify aliNotify = new AlipayClass.AlipayNotify(sArrary, Request.Form["notify_id"], partner, key, input_charset, sign_type, transport);
                string responseTxt = aliNotify.ResponseTxt; //获取远程服务器ATN结果，验证是否是支付宝服务器发来的请求
                string sign = Request.Form["sign"];         //获取支付宝反馈回来的sign结果
                string mysign = aliNotify.Mysign;           //获取通知返回后计算后（验证）的签名结果

                //写日志记录（若要调试，请取消下面两行注释）
                //string sWord = "responseTxt=" + responseTxt + "\n notify_url_log:sign=" + Request.Form["sign"] + "&mysign=" + mysign + "\n notify回来的参数：" + aliNotify.PreSignStr;
                //AlipayFunction.log_result(Server.MapPath("log/" + DateTime.Now.ToString().Replace(":", "")) + ".txt", sWord);

                //判断responsetTxt是否为ture，生成的签名结果mysign与获得的签名结果sign是否一致
                //responsetTxt的结果不是true，与服务器设置问题、合作身份者ID、notify_id一分钟失效有关
                //mysign与sign不等，与安全校验码、请求时的参数格式（如：带自定义参数等）、编码格式有关
                if (responseTxt == "true" && sign == mysign)//验证成功
                {
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //请在这里加上商户的业务逻辑程序代码

                    //——请根据您的业务逻辑来编写程序（以下代码仅作参考）——
                    //获取支付宝的通知返回参数，可参考技术文档中服务器异步通知参数列表
                    string trade_no = Request.Form["trade_no"];         //支付宝交易号
                    string order_no = Request.Form["out_trade_no"];     //获取订单号
                    string total_fee = Request.Form["price"];           //获取总金额
                    string subject = Request.Form["subject"];           //商品名称、订单名称
                    string body = Request.Form["body"];                 //商品描述、订单备注、描述
                    string buyer_email = Request.Form["buyer_email"];   //买家支付宝账号
                    string trade_status = Request.Form["trade_status"]; //交易状态

                    if (Request.Form["trade_status"] == "WAIT_BUYER_PAY")
                    {//该判断表示买家已在支付宝交易管理中产生了交易记录，但没有付款

                        //判断该笔订单是否在商户网站中已经做过处理（可参考“集成教程”中“3.4返回数据处理”）
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序

                        return Content("success");  //请不要修改或删除
                    }
                    else if (Request.Form["trade_status"] == "WAIT_SELLER_SEND_GOODS")
                    {//该判断示买家已在支付宝交易管理中产生了交易记录且付款成功，但卖家没有发货

                        //判断该笔订单是否在商户网站中已经做过处理（可参考“集成教程”中“3.4返回数据处理”）
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序
                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", order_no);

                        this.UpdatePayStatus(orderId);

                        return Content("success");  //请不要修改或删除
                    }
                    else if (Request.Form["trade_status"] == "WAIT_BUYER_CONFIRM_GOODS")
                    {//该判断表示卖家已经发了货，但买家还没有做确认收货的操作

                        //判断该笔订单是否在商户网站中已经做过处理（可参考“集成教程”中“3.4返回数据处理”）
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序

                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", order_no);

                        this.UpdatePayStatus(orderId);

                        return Content("success");  //请不要修改或删除
                    }
                    else if (Request.Form["trade_status"] == "TRADE_FINISHED")
                    {//该判断表示买家已经确认收货，这笔交易完成

                        //判断该笔订单是否在商户网站中已经做过处理（可参考“集成教程”中“3.4返回数据处理”）
                        //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                        //如果有做过处理，不执行商户的业务程序

                        var orderId = SL.Data.SQL.QueryValue<int>("select OrderID from OrderInfo where OrderCode=@p0", order_no);

                        this.UpdatePayStatus(orderId);

                        //orderBLL.ConfirmGoods(orderId);

                        return Content("success");  //请不要修改或删除
                    }
                    else
                    {
                        return Content("success");  //其他状态判断。普通即时到帐中，其他状态不用判断，直接打印success。
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
