using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace XX_PhotoPrint.Service
{
    public class ProductService
    {
        public static IList<dynamic> Search(int categoryId, string keywords, int page, int pageSize, string sort, string sortType, out int total)
        {
            return Search(categoryId, 0, keywords, page, pageSize, sort, sortType, out  total);
        }

        public static IList<dynamic> Search(string keywords, int page, int pageSize, string sort, string sortType, out int total)
        {
            return Search(0, 0, keywords, page, pageSize, sort, sortType, out  total);
        }

        public static IList<dynamic> Search(string keywords, int page, int pageSize, out int total)
        {
            return Search(0, 0, keywords, page, pageSize, null, null, out  total);
        }

        public static IList<dynamic> Search(int categoryId, int subId, string keywords, int page, int pageSize, string sort, string sortType, out int total)
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