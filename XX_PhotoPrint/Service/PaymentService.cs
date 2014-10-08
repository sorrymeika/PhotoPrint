using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XX_PhotoPrint.Service
{
    public class PaymentService
    {
        public static readonly int AlipayWap = 1;
        public static List<Dictionary<string, object>> Payments = new List<Dictionary<string, object>>
        {
            new Dictionary<string,object> {
                { "PaymentID", AlipayWap },
                { "PaymentName", "支付宝手机网页支付" },
            },
            new Dictionary<string,object> {
                { "PaymentID", 2 },
                { "PaymentName", "支付宝" },
            }
        };
    }
}