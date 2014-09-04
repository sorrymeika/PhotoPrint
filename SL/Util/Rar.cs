using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

namespace INAnswer.Service
{
    public class Rar
    {
        public static void Decompress(string rarPath, string descDir)
        {
            if (!System.IO.File.Exists(rarPath))
                return;

            string rar = HttpContext.Current.Server.MapPath("~/Content/Rar.exe");
            string arguments = " X -o+ " + rarPath + " " + descDir;

            var processStartInfo = new ProcessStartInfo(rar, arguments);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();
                string line;
                while (true)
                {
                    line = process.StandardOutput.ReadLine();
                    if (line == "全部OK")
                    {
                        break;
                    }
                    else if (line == null)
                    {
                        break;
                    }
                }
            }
        }

        public static void Compress(string dir, string descPath)
        {
            if (!System.IO.Directory.Exists(dir))
                return;

            string rar = HttpContext.Current.Server.MapPath("~/Content/Rar.exe");
            string arguments = " a -r -o+ " + descPath;

            var processStartInfo = new ProcessStartInfo(rar, arguments);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.WorkingDirectory = dir;

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();
                string line;
                while (true)
                {
                    line = process.StandardOutput.ReadLine();
                    if (line == "完成")
                    {
                        break;
                    }
                    else if (line == null)
                    {
                        break;
                    }
                }
            }
        }
    }
}