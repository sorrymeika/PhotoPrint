using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using Excel = Microsoft.Office.Interop.Excel;
using word = Microsoft.Office.Interop.Word;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Text;

namespace INAnswer.Service
{
    public class ExcelService
    {
        public static string ExcelToHtml(string excelFileName)
        {
            //实例化Excel  
            Microsoft.Office.Interop.Excel.Application repExcel = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbook workbook = null;
            Microsoft.Office.Interop.Excel.Worksheet worksheet = null;
            //打开文件，n.FullPath是文件路径  
            workbook = repExcel.Application.Workbooks.Open(excelFileName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            worksheet = (Microsoft.Office.Interop.Excel.Worksheet)workbook.Worksheets[1];
            string filesavefilename = excelFileName.ToString();
            string strsavefilename = filesavefilename.Substring(0, filesavefilename.LastIndexOf('.')) + ".html";
            object savefilename = (object)strsavefilename;
            object ofmt = Microsoft.Office.Interop.Excel.XlFileFormat.xlHtml;
            //进行另存为操作    
            workbook.SaveAs(savefilename, ofmt, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            object osave = false;
            //逐步关闭所有使用的对象  
            workbook.Close(osave, Type.Missing, Type.Missing);
            repExcel.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
            worksheet = null;
            //垃圾回收  
            GC.Collect();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
            workbook = null;
            GC.Collect();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(repExcel.Application.Workbooks);
            GC.Collect();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(repExcel);
            repExcel = null;
            GC.Collect();
            //依据时间杀灭进程  
            System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName("EXCEL");
            foreach (System.Diagnostics.Process p in process)
            {
                if (DateTime.Now.Second - p.StartTime.Second > 0 && DateTime.Now.Second - p.StartTime.Second < 5)
                {
                    p.Kill();
                }
            }

            return savefilename.ToString();
        }


        public static string WordToHtml(object wordfilename)
        {
            //在此处放置用户代码以初始化页面   
            word.Application word = new word.Application();
            Type wordtype = word.GetType();
            word.Documents docs = word.Documents;
            //打开文件   
            Type docstype = docs.GetType();
            word.Document doc = (word.Document)docstype.InvokeMember("open", System.Reflection.BindingFlags.InvokeMethod, null, docs, new object[] { wordfilename, true, true });
            //转换格式，另存为   
            Type doctype = doc.GetType();
            string wordsavefilename = wordfilename.ToString();
            string strsavefilename = wordsavefilename.Substring(0, wordsavefilename.LastIndexOf('.')) + ".html";
            object savefilename = (object)strsavefilename;
            doctype.InvokeMember("saveas", System.Reflection.BindingFlags.InvokeMethod, null, doc, new object[] { savefilename, Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatFilteredHTML });
            doctype.InvokeMember("close", System.Reflection.BindingFlags.InvokeMethod, null, doc, null);
            // 退出 word   
            wordtype.InvokeMember("quit", System.Reflection.BindingFlags.InvokeMethod, null, word, null);
            return savefilename.ToString();
        }


        public static string Import(string excelPath, string contentDir, out bool resultFlag)
        {
            resultFlag = false;

            File.Delete(excelPath.Substring(0, excelPath.LastIndexOf('.')) + ".html");
            string htmlDir = excelPath.Substring(0, excelPath.LastIndexOf('.')) + ".files";

            if (Directory.Exists(htmlDir))
                Directory.Delete(htmlDir, true);

            ExcelToHtml(excelPath);

            string htmlPath = htmlDir + "/sheet001.html";
            string html;

            string excelDir = Path.GetDirectoryName(excelPath);

            using (StreamReader sr = new StreamReader(File.OpenRead(htmlPath), System.Text.Encoding.GetEncoding("gb2312")))
            {
                html = sr.ReadToEnd();
            }

            List<string> images = new List<string>();
            for (var m = Regex.Match(html, @"\<v\:imagedata src\=""([^""]+?)"" o\:title\=""[^""]*?""/\>"); m.Success; m = m.NextMatch())
            {
                images.Add(htmlDir + "/" + m.Groups[1].Value);
            }

            Excel.Application excel = new Excel.Application();//引用Excel对象
            Excel.Workbook workbook = excel.Workbooks.Add(excelPath);
            excel.UserControl = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            excel.Visible = false;

            //从1开始.
            Excel.Worksheet sheet = workbook.Worksheets.get_Item(1) as Excel.Worksheet;

            int StartRow = 2;

            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            Dictionary<int, string> map = new Dictionary<int, string> { 
                { 1,"AnswerCode" },
                { 2,"AnswerName" },
                { 3,"CategoryName" },
                { 4,"GradeName" },
                { 5,"SubjectName" },
                { 6,"Cover" },
                { 7,"CatalogName" },
                { 8,"UnitName" },
                { 9,"Content" },
            };

            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            for (int row = StartRow; row <= sheet.UsedRange.Rows.Count; row++)
            {
                //取单元格值；
                Dictionary<string, string> data = new Dictionary<string, string>();
                for (int col = 1; col <= sheet.UsedRange.Columns.Count; col++)
                {
                    string key = map[col];
                    Excel.Range range = sheet.Cells[row, col] as Excel.Range;
                    data[key] = range.Text.Trim();


                }
                list.Add(data);
            }

            using (Database db = Database.Open())
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    string code = item["AnswerCode"];
                    Dictionary<string, object> data = result.FirstOrDefault(a => a["AnswerCode"].ToString() == code);
                    if (data == null)
                    {
                        data = new Dictionary<string, object> { 
                            { "AnswerCode", code },
                            { "AnswerName", item["AnswerName"] },
                            { "CategoryName",item["CategoryName"] },
                            { "GradeName", item["GradeName"] },
                            { "SubjectName", item["SubjectName"] },
                            { "Content", item["Content"] },
                            { "Catalog", new List<Dictionary<string,object>>() },
                        };

                        #region
                        if (db.Exists("select 1 from Answer where Code=@p0", code))
                        {
                            return code + "," + item["AnswerName"] + "已有相同的书号，未执行任何操作！";
                        }

                        int categoryId = db.QueryValue<int>("select CategoryID from Category where CategoryName=@p0", item["CategoryName"]);
                        if (categoryId == 0)
                        {
                            return "类别“" + item["CategoryName"] + "”不存在，未执行任何操作！";
                        }
                        data["CategoryID"] = categoryId;

                        if (!string.IsNullOrEmpty(item["GradeName"]))
                        {
                            int gradeId = db.QueryValue<int>("select GradeID from Grade where GradeName=@p0", item["GradeName"]);
                            if (gradeId == 0)
                            {
                                return "年级“" + item["GradeName"] + "”不存在，未执行任何操作！";
                            }
                            data["GradeID"] = gradeId;
                        }
                        else
                        {
                            data["GradeID"] = 0;
                        }

                        if (!string.IsNullOrEmpty(item["SubjectName"]))
                        {
                            int gradeId = db.QueryValue<int>("select SubjectID from [Subject] where SubjectName=@p0", item["SubjectName"]);
                            if (gradeId == 0)
                            {
                                return "科目“" + item["SubjectName"] + "”不存在，未执行任何操作！";
                            }
                            data["SubjectID"] = gradeId;
                        }
                        else
                        {
                            data["SubjectID"] = 0;
                        }

                        if (string.IsNullOrEmpty(item["Content"]))
                        {
                            return code + "," + item["AnswerName"] + "内容为空，未执行任何操作！";
                        }

                        string s = images[result.Count];

                        string src = "content/books/" + code + ".jpg";
                        string savePath = contentDir + "\\books\\" + code + ".jpg";

                        if (!Directory.Exists(contentDir + "\\books"))
                        {
                            Directory.CreateDirectory(contentDir + "\\books");
                        }

                        File.Copy(s, savePath, true);

                        data["Cover"] = src;

                        result.Add(data);

                        #endregion
                    }

                    List<Dictionary<string, object>> catalogList = (List<Dictionary<string, object>>)data["Catalog"];

                    Dictionary<string, object> catalog = catalogList.FirstOrDefault(a => a["CatalogName"].ToString() == item["CatalogName"]);

                    if (catalog == null)
                    {
                        catalog = new Dictionary<string, object> { 
                        { "CatalogName", item["CatalogName"] },
                        { "Unit", new List<Dictionary<string, object>>() }
                    };
                        catalogList.Add(catalog);
                    }

                    List<Dictionary<string, object>> unitList = (List<Dictionary<string, object>>)catalog["Unit"];

                    unitList.Add(new Dictionary<string, object> { 
                        { "UnitName", item["UnitName"] },
                        { "Content", item["Content"] }
                    });
                }

                for (var i = 0; i < result.Count; i++)
                {
                    var data = result[i];

                    int answerId;

                    db.Execute("insert into Answer (Title,SubjectID,CategoryID,Cover,PublishTime,[File],GradeID,Code) values (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)", out answerId, data["AnswerName"], data["SubjectID"], data["CategoryID"], data["Cover"], DateTime.Now, "", data["GradeID"], data["AnswerCode"]);

                    List<Dictionary<string, object>> catalogList = (List<Dictionary<string, object>>)data["Catalog"];

                    foreach (var catalog in catalogList)
                    {
                        int catalogId;
                        db.Execute("insert into [Catalog] (AnswerID,CatalogName) values (@p0,@p1)", out catalogId, answerId, catalog["CatalogName"]);

                        List<Dictionary<string, object>> unitList = (List<Dictionary<string, object>>)catalog["Unit"];
                        foreach (var unit in unitList)
                        {
                            #region
                            string doc = unit["Content"].ToString();

                            if (Regex.IsMatch(doc, @"^\d+\.(docx|doc)$"))
                            {
                                string docPath = excelDir + "/" + doc;

                                if (File.Exists(docPath))
                                {
                                    string docDir = Path.GetDirectoryName(docPath);

                                    File.Delete(docPath.Substring(0, docPath.LastIndexOf('.')) + ".html");
                                    htmlDir = docPath.Substring(0, docPath.LastIndexOf('.')) + ".files";

                                    if (Directory.Exists(htmlDir))
                                        Directory.Delete(htmlDir, true);

                                    htmlPath = WordToHtml(docPath);

                                    using (StreamReader sr = new StreamReader(File.OpenRead(htmlPath), System.Text.Encoding.GetEncoding("gb2312")))
                                    {
                                        html = sr.ReadToEnd();
                                    }

                                    var m1 = Regex.Match(html, "<body[^>]*?>");
                                    var m2 = Regex.Match(html, "</body>");

                                    string docName = docPath.Substring(docPath.LastIndexOf('\\') + 1, docPath.Length - docPath.LastIndexOf('.'));

                                    html = html.Substring(m1.Index + m1.Length, m2.Index - m1.Index - m1.Length);

                                    html = Regex.Replace(html, "<p[^>]+>", "<p>", RegexOptions.IgnoreCase);
                                    html = Regex.Replace(html, "<span[^>]*>|</span>", "", RegexOptions.IgnoreCase);
                                    html = Regex.Replace(html, "<div[^>]*>|</div>", "", RegexOptions.IgnoreCase);
                                    html = Regex.Replace(html, @"<\!--.*?-->", "", RegexOptions.IgnoreCase);


                                    string timeDir = DateTime.Now.ToString("yyyyMMddHHmmssffff") + docName;
                                    string srcDir = "/content/books/" + timeDir;
                                    string imageDir = contentDir + "\\books\\" + timeDir;
                                    if (!Directory.Exists(imageDir))
                                        Directory.CreateDirectory(imageDir);

                                    html = Regex.Replace(html, @"\s+src\=""([^""]+?)""\s+", new MatchEvaluator(m3 =>
                                    {
                                        string path = m3.Groups[1].Value;

                                        path = docDir + "\\" + path.Replace("/", "\\");

                                        if (!File.Exists(Path.Combine(imageDir, Path.GetFileName(path))))
                                            File.Copy(path, Path.Combine(imageDir, Path.GetFileName(path)));

                                        string src = srcDir + "/" + Path.GetFileName(path);

                                        return " src=\"" + src + "\" ";
                                    }), RegexOptions.IgnoreCase);

                                    html = html.Replace("\r", " ").Replace("\n", " ");
                                    html = Regex.Replace(html, @"\s{2,}", " ", RegexOptions.IgnoreCase);

                                    html = Regex.Replace(html, @"(width|height)=\d+", "", RegexOptions.IgnoreCase);
                                    html = Regex.Replace(html, @"(width|height)=""\d+""", "", RegexOptions.IgnoreCase);

                                    unit["Content"] = html;
                                }
                            }
                            else
                            {
                                unit["Content"] = doc.Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
                            }
                            #endregion

                            db.Execute("insert into [Unit] (AnswerID,UnitName,CatalogID,Content,PublishTime) values (@p0,@p1,@p2,@p3,@p4)", answerId, unit["UnitName"], catalogId, unit["Content"], DateTime.Now);
                        }
                    }
                }
            }

            resultFlag = true;
            return sb.ToString();

        }
    }
}