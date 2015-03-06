using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;

namespace XX_PhotoPrint.Service
{
    public class ProductService
    {
        public static dynamic GetByID(int workId)
        {
            var Request = HttpContext.Current.Request;

            using (SL.Data.Database db = SL.Data.Database.Open())
            {
                var data = db.QuerySingle("select w.WorkID,w.WorkName,w.WorkDesc,w.ProductID,w.Swf,a.ProductName,a.Price,w.SoldNum,a.BaseInfo,a.Content,a.SubID,b.SubName,c.CategoryID,c.CategoryName from Work w join Product a on w.ProductID=a.ProductID join SubCate b on a.SubID=b.SubID join Category c on b.CategoryID=c.CategoryID where w.WorkID=@p0 and w.Deleted=0 and a.Deleted=0", workId);
                if (data == null)
                {
                    return null;
                }
                int productId = (int)data["ProductID"];
                string baseInfo = (string)data["BaseInfo"];
                if (!string.IsNullOrEmpty(baseInfo))
                {
                    data["BaseInfo"] = Json.Decode<IList<IDictionary<string, object>>>(baseInfo);
                }
                else if (baseInfo == "")
                {
                    data["BaseInfo"] = null;
                }

                if (!string.IsNullOrEmpty((string)data["Content"]))
                {
                    data["Content"] = System.Text.RegularExpressions.Regex.Replace(data["Content"].ToString(), @"src=""/", "src=\"http://" + Request.Url.Authority + "/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                data["Colors"] = db.Query("select ColorID,ColorName,ColorCode from Color where ProductID=@p0", productId);
                data["Styles"] = db.Query("select a.StyleID,StyleName,Rect,ColorID,SizeID,[Print],Content from Style a left join Customization b on a.StyleID=b.StyleID where WorkID=@p0 order by a.StyleID", workId);
                data["Size"] = db.Query("select SizeID,SizeName,StyleID from ProductSize where ProductID=@p0", productId);
                data["StyleColorPic"] = db.Query("select PicID,StyleID,ColorID,Picture from StyleColorPic where ProductID=@p0", productId);

                return data;
            }
        }

        public static IList<dynamic> Search(int categoryId, string keywords, int page, int pageSize, string sort, string sortType, out int total)
        {
            return Search(categoryId, 0, -1, keywords, page, pageSize, sort, sortType, out  total);
        }

        public static IList<dynamic> Search(string keywords, int page, int pageSize, string sort, string sortType, out int total)
        {
            return Search(0, 0, -1, keywords, page, pageSize, sort, sortType, out  total);
        }

        public static IList<dynamic> Search(string keywords, int page, int pageSize, out int total)
        {
            return Search(0, 0, -1, keywords, page, pageSize, null, null, out  total);
        }

        public static IList<dynamic> Search(string keywords, int productType, int page, int pageSize, out int total)
        {
            return Search(0, 0, productType, keywords, page, pageSize, null, null, out  total);
        }

        public static IList<dynamic> Search(int categoryId, int subId, int productType, string keywords, int page, int pageSize, string sort, string sortType, out int total)
        {
            string where = "a.Deleted=0 and b.Deleted=0";
            List<object> parameters = new List<object>();
            if (subId != 0)
            {
                where += " and d.SubID=@p0";
                parameters.Add(subId);
            }
            else if (categoryId != 0)
            {
                where += " and d.CategoryID=@p" + parameters.Count;
                parameters.Add(categoryId);
            }

            if (productType >= 0)
            {
                where += " and b.ProductType=@p" + parameters.Count;
                parameters.Add(productType);
            }

            if (!string.IsNullOrEmpty(keywords))
            {
                where += " and (ProductName like '%'+@p" + parameters.Count + "+'%' or Tag like '%'+@p" + parameters.Count + "+'%' or WorkName like '%'+@p" + parameters.Count + "+'%')";
                parameters.Add(keywords);
            }

            IDictionary<string, bool> sortDic = new Dictionary<string, bool>{
                {"b.Sort",false}
            };

            if ("sold".Equals(sort, StringComparison.OrdinalIgnoreCase))
            {
                sortDic.Add("SoldNum", sortType == "asc");
            }
            else if ("date".Equals(sort, StringComparison.OrdinalIgnoreCase))
            {
                sortDic.Add("CreationTime", sortType == "asc");
            }
            else
            {
                sortDic.Add("CreationTime", false);
            }

            var data = SL.Data.SQL.QueryPage(new[] { "WorkID" },
                "WorkID,WorkName,a.ProductID,b.ProductName,a.Picture,b.Price,b.SubID,d.SubName,d.CategoryID,c.CategoryName",
                "Work a join Product b on a.ProductID=b.ProductID join SubCate d on b.SubID=d.SubID join Category c on d.CategoryID=c.CategoryID",
                where, page, pageSize, parameters.ToArray(), out total, sortDic);

            string url = "http://" + HttpContext.Current.Request.Url.Authority + "/Content/";
            if (data != null)
            {
                data.All(a =>
                {
                    a.Picture = url + a.Picture;
                    return true;
                });
            }

            return data;
        }

        public static IList<dynamic> GetAll()
        {
            return SL.Data.SQL.Query("select WorkID,WorkName,a.ProductID,b.ProductName,a.Picture,b.Price,b.SubID,d.SubName,d.CategoryID,c.CategoryName from Work a join Product b on a.ProductID=b.ProductID join SubCate d on b.SubID=d.SubID join Category c on d.CategoryID=c.CategoryID order by WorkID desc");
        }

    }
}