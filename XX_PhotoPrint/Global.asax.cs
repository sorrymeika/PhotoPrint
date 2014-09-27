﻿using System;
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
                "User",
                "{handle}.html",
                new { controller = "Home", action = "Index", catalog = "User" },
                new { handle = "^Login|Register|Register1|Register2|UserCenter|UserInfo$" }

            );

            routes.MapRoute(
                "UserShop",
                "{handle}",
                new { controller = "Home", action = "Index", catalog = "User" },
                new { handle = "^Cart$" }

            );

            routes.MapRoute(
                "Item",
                "item/{id}.html",
                new { controller = "Home", action = "Index", catalog = "Product", handle = "Item" },
                new { id = "^\\d+$" }
            );

            routes.MapRoute(
                "JsonDefault",
                "json/{catalog}/{handle}",
                new { controller = "Home", action = "JsonAction", catalog = "Home", handle = "Index" }
            );

            routes.MapRoute(
                "AlipayPayCallback",
                "pay/alipaycallback",
                new { controller = "Home", action = "AlipayPayCallback" }
            );

            routes.MapRoute(
                "AlipayPayNotify",
                "pay/alipaynotify",
                new { controller = "Home", action = "AlipayPayNotify" }
            );

            routes.MapRoute(
              "AlipayPaySuccess",
              "pay/success",
              new { controller = "Home", action = "AlipayPaySuccess" }
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