using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XX_PhotoPrint
{
    public class Config
    {
        public static readonly string Alipay_PrivateKey = System.Configuration.ConfigurationManager.AppSettings["Alipay_PrivateKey"];
        public static readonly string Alipay_Partner = System.Configuration.ConfigurationManager.AppSettings["Alipay_Partner"];
        public static readonly string Alipay_Seller_account_name = System.Configuration.ConfigurationManager.AppSettings["Alipay_Seller_account_name"];
        public static readonly string Alipaypublick = System.Configuration.ConfigurationManager.AppSettings["Alipaypublick"];

        public static readonly string AlipayKey =System.Configuration.ConfigurationManager.AppSettings["AlipayKey"];
    }
}