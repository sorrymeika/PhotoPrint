using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace XX_PhotoPrint
{
    // 注意: 有关启用 IIS6 或 IIS7 经典模式的说明，
    // 请访问 http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "UserHtml",
                "{handle}.html",
                new { controller = "Home", action = "Index", catalog = "User" },
                new { handle = "^Login|Register|Register1|Register2|UserCenter|UserInfo$" }
            );

            routes.MapRoute(
                "User",
                "{handle}",
                new { controller = "Home", action = "Index", catalog = "User" },
                new { handle = "^signout$" }
            );


            routes.MapRoute(
                "Shop",
                "{handle}",
                new { controller = "Home", action = "Index", catalog = "Order" },
                new { handle = "^Cart|Order$" }
            );

            routes.MapRoute(
                "ShopItem",
                "{handle}/{id}",
                new { controller = "Home", action = "Index", catalog = "Order" },
                new { handle = "^Order$", id = "^\\d+$" }
            );

            routes.MapRoute(
                "Gallery",
                "{handle}.html",
                new { controller = "Home", action = "Index", catalog = "Product" },
                new { handle = "^Gallery$" }
            );

            routes.MapRoute(
                "Item",
                "{handle}/{id}.html",
                new { controller = "Home", action = "Index", catalog = "Product" },
                new { handle = "^Gallery|Item$", id = "^\\d+$" }
            );

            routes.MapRoute(
                "News",
                "{handle}.html",
                new { controller = "Home", action = "Index", catalog = "News" },
                new { handle = "^Recruit|Download|AboutUs|Contact|Help$" }
            );

            routes.MapRoute(
                "NewsItem",
                "{handle}/{id}.html",
                new { controller = "Home", action = "Index", catalog = "News" },
                new { handle = "^Recruit$", id = "^\\d+$" }
            );

            routes.MapRoute(
                "Home",
                "{handle}.html",
                new { controller = "Home", action = "Index", catalog = "Home" },
                new { handle = "^eco$" }
            );


            routes.MapRoute(
                "JsonDefault",
                "json/{catalog}/{handle}",
                new { controller = "Home", action = "JsonAction", catalog = "Home", handle = "Index" }
            );

            routes.MapRoute(
                "alipayto",
                "alipayto/{orderid}",
                new { controller = "Home", action = "alipayto" },
                new { orderid = "^\\d+$" }
            );

            routes.MapRoute(
                "AlipayReturnUrl",
                "AlipayReturnUrl",
                new { controller = "Home", action = "AlipayReturnUrl" }
            );

            routes.MapRoute(
                "AlipayNotifyUrl",
                "AlipayNotifyUrl",
                new { controller = "Home", action = "AlipayNotifyUrl" }
            );

            routes.MapRoute(
                "AlipayPayCallback",
                "pay/alipaycallback/{orderid}",
                new { controller = "Home", action = "AlipayPayCallback" },
                new { orderid = "^\\d+$" }
            );

            routes.MapRoute(
                "AlipayPayNotify",
                "pay/alipaynotify/{orderid}",
                new { controller = "Home", action = "AlipayPayNotify" },
                new { orderid = "^\\d+$" }
            );

            routes.MapRoute(
              "AlipayPaySuccess",
              "pay/success/{orderid}",
              new { controller = "Home", action = "AlipayPaySuccess" },
              new { orderid = "^\\d+$" }
            );

            routes.MapRoute(
                "Pay",
                "pay/{orderid}",
                new { controller = "Home", action = "Pay", orderid = 0 },
                new { orderid = @"^\d+$" }
            );

            routes.MapRoute(
                "ImagePreview",
                "ImagePreview",
                new { controller = "Home", action = "ImagePreview" }
            );

            routes.MapRoute(
               "Upload",
               "manage/upload",
               new { controller = "Home", action = "Upload" }
           );

            routes.MapRoute(
                "Manage",
                "manage/{catalog}/{handle}",
                new { controller = "Home", action = "Manage", catalog = "Home", handle = "" }
            );

            routes.MapRoute(
                "CheckCode",
                "CheckCode/{id}.jpg",
                new { controller = "Home", action = "CheckCode", id = @"\d+" }
            );

            routes.MapRoute(
                "Default",
                "{catalog}/{handle}",
                new { controller = "Home", action = "Index", catalog = "Home", handle = "Index" }
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}