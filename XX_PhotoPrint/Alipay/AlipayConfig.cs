using System.Web;
using System.Text;
using System.IO;
using System.Net;
using System;
using System.Collections.Generic;

namespace Alipay.Class
{
    /// <summary>
    /// 类名：Config
    /// 功能：支付宝配置公共类
    /// 详细：该类是配置所有请求参数，支付宝网关、接口，商户的基本参数等
    /// 版本：3.0
    /// 日期：2012-07-11
    /// 说明：
    /// 以下代码只是为了方便商户测试而提供的样例代码，商户可以根据自己网站的需要，按照技术文档编写,并非一定要使用该代码。
    /// 该代码仅供学习和研究支付宝接口使用，只是提供一个参考。
    /// </summary>
    public class AlipayConfig
    {
        static AlipayConfig()
        {
            #region 支付宝参数，必须按照以下值传递
            Req_url = "http://wappaygw.alipay.com/service/rest.htm";

            V = "2.0";
            Service_Create = "alipay.wap.trade.create.direct";
            Service_Auth = "alipay.wap.auth.authAndExecute";
            Sec_id = "0001";
            Format = "xml";
            Input_charset_UTF8 = "UTF-8";
            #endregion

            #region 商户需要手动配置
            Partner = "";
            Seller_account_name = "";

            PrivateKey = "";

            Req_id = System.Guid.NewGuid().ToString("N");
            //Out_trade_no = System.DateTime.Now.ToString();
            Subject = "商品";
            //Total_fee = "";
            //Out_user = "";

            //三个返回URL
            Call_back_url = "";
            Notify_url = "";
            Merchant_url = "";

            #endregion
        }

        #region 属性
        /// <summary>
        /// 请求ID 请随机生成
        /// </summary>
        public static string Req_id
        {
            get;
            set;
        }
        /// <summary>
        /// 请求地址
        /// </summary>
        public static string Req_url
        {
            get;
            set;
        }
        /// <summary>
        /// 版本
        /// </summary>
        public static string V
        {
            get;
            set;
        }
        /// <summary>
        /// 创建交易网关
        /// </summary>
        public static string Service_Create
        {
            get;
            set;
        }
        /// <summary>
        /// 执行授权网关
        /// </summary>
        public static string Service_Auth
        {
            get;
            set;
        }
        /// <summary>
        /// 商户ID
        /// </summary>
        public static string Partner
        {
            get;
            set;
        }
        /// <summary>
        /// 签名类型
        /// </summary>
        public static string Sec_id
        {
            get;
            set;
        }
        /// <summary>
        /// 支付宝公钥
        /// </summary>
        public static string Alipaypublick
        {
            get;
            set;
        }
        /// <summary>
        /// 商户私钥
        /// </summary>
        public static string PrivateKey
        {
            get;
            set;
        }
        /// <summary>
        /// 请求参数格式
        /// </summary>
        public static string Format
        {
            get;
            set;
        }
        /// <summary>
        /// 用户付款成功同步返回URL
        /// </summary>
        public static string Call_back_url
        {
            get;
            set;
        }
        /// <summary>
        /// 外部交易号(由商户创建，请不要重复)
        /// </summary>
        //public static string Out_trade_no
        //{
        //    get;
        //    set;
        //}
        /// <summary>
        /// 订单标题
        /// </summary>
        public static string Subject
        {
            get;
            set;
        }
        /// <summary>
        /// 订单价格
        /// </summary>
        //public static string Total_fee
        //{
        //    get;
        //    set;
        //}
        /// <summary>
        /// 卖家账户名称
        /// </summary>
        public static string Seller_account_name
        {
            get;
            set;
        }
        /// <summary>
        /// 外部用户唯一标识
        /// </summary>
        //public static string Out_user
        //{
        //    get;
        //    set;
        //}
        /// <summary>
        /// 服务端接收通知URL
        /// </summary>
        public static string Notify_url
        {
            get;
            set;
        }
        /// <summary>
        /// 用户付款中途退出返回URL
        /// </summary>
        public static string Merchant_url
        {
            get;
            set;
        }
        /// <summary>
        /// 编码格式UTF-8
        /// </summary>
        public static string Input_charset_UTF8
        {
            get;
            set;
        }
        #endregion
    }
}