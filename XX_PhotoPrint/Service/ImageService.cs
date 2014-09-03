using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace XX_PhotoPrint.Service
{
    public class ImageService
    {
        private static Size getNewSize(int maxWidth, int maxHeight, int imageOriginalWidth, int imageOriginalHeight)
        {
            double w = 0.0;
            double h = 0.0;
            double sw = Convert.ToDouble(imageOriginalWidth);
            double sh = Convert.ToDouble(imageOriginalHeight);
            double mw = Convert.ToDouble(maxWidth);
            double mh = Convert.ToDouble(maxHeight);

            if (sw < mw && sh < mh)
            {
                w = sw;
                h = sh;
            }
            else if ((sw / sh) > (mw / mh))
            {
                w = maxWidth;
                h = (w * sh) / sw;
            }
            else
            {
                h = maxHeight;
                w = (h * sw) / sh;
            }

            return new Size(Convert.ToInt32(w), Convert.ToInt32(h));
        }


        public static System.Drawing.Image GetThumbNailImage(System.Drawing.Image originalImage, int thumMaxWidth, int thumMaxHeight)
        {
            Size thumRealSize = Size.Empty;
            System.Drawing.Image newImage = originalImage;
            Graphics graphics = null;

            try
            {
                thumRealSize = getNewSize(thumMaxWidth, thumMaxHeight, originalImage.Width, originalImage.Height);
                newImage = new Bitmap(thumRealSize.Width, thumRealSize.Height);
                graphics = Graphics.FromImage(newImage);

                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.Clear(Color.Transparent);

                graphics.DrawImage(originalImage, new Rectangle(0, 0, thumRealSize.Width, thumRealSize.Height), new Rectangle(0, 0, originalImage.Width, originalImage.Height), GraphicsUnit.Pixel);
            }
            catch { }
            finally
            {
                if (graphics != null)
                {
                    graphics.Dispose();
                    graphics = null;
                }
            }

            return newImage;
        }

        public static byte[] GetThumbNailImageBytes(Stream stream, int thumMaxWidth, int thumMaxHeight)
        {
            var image = GetThumbNailImage(System.Drawing.Image.FromStream(stream), thumMaxWidth, thumMaxHeight);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                image.Dispose();

                return ms.ToArray();
            }
        }

        public static byte[] GetThumbNailImageFromFile(string fileName, int thumMaxWidth, int thumMaxHeight)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                return GetThumbNailImageBytes(fs, thumMaxWidth, thumMaxHeight);
            }
        }

        #region 验证码
        public static string CreateRandomCode(int codeCount)
        {
            string allChar = "1,2,3,4,5,6,7,8,9,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
            string[] allCharArray = allChar.Split(',');
            string randomCode = "";
            int temp = -1;
            Random rand = new Random();
            for (int i = 0; i < codeCount; i++)
            {
                if (temp != -1)
                {
                    rand = new Random(temp * i * ((int)DateTime.Now.Ticks));
                }

                int t = rand.Next(allCharArray.Length - 1);

                while (temp == t)
                {
                    t = rand.Next(allCharArray.Length - 1);
                }

                temp = t;
                randomCode += allCharArray[t];
            }
            return randomCode;
        }

        public static string CreateSimpleRandomCode(int codeCount)
        {
            string allChar = "2,3,4,5,6,7,8,9,A,B,C,D,E,F,G,H,J,K,M,N,P,Q,R,S,T,U,V,W,X,Y,Z";
            string[] allCharArray = allChar.Split(',');
            string randomCode = "";
            int temp = -1;
            Random rand = new Random();
            for (int i = 0; i < codeCount; i++)
            {
                if (temp != -1)
                {
                    rand = new Random(temp * i * ((int)DateTime.Now.Ticks));
                }

                int t = rand.Next(allCharArray.Length - 1);

                while (temp == t)
                {
                    t = rand.Next(allCharArray.Length - 1);
                }

                temp = t;
                randomCode += allCharArray[t];
            }
            return randomCode;
        }

        public static byte[] CreateImage(out string checkCode)
        {
            checkCode = CreateRandomCode(4);

            int iwidth = (int)(checkCode.Length * 12.5);
            Bitmap image = new Bitmap(iwidth, 22);
            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);
            //定义颜色
            Color[] c = { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Brown, Color.DarkCyan, Color.Purple };
            //定义字体 
            string[] font = { "Times New Roman", "Microsoft Sans Serif", "MS Mincho", "Book Antiqua", "PMingLiU" };
            Random rand = new Random();
            //随机输出噪点
            for (int i = 0; i < 50; i++)
            {
                int x = rand.Next(image.Width);
                int y = rand.Next(image.Height);
                g.DrawRectangle(new Pen(Color.LightGray, 0), x, y, 1, 1);
            }

            //输出不同字体和颜色的验证码字符
            for (int i = 0; i < checkCode.Length; i++)
            {
                int cindex = rand.Next(7);
                int findex = rand.Next(5);

                Font f = new System.Drawing.Font(font[findex], 10, System.Drawing.FontStyle.Bold);
                Brush b = new System.Drawing.SolidBrush(c[cindex]);
                int ii = 4;
                if ((i + 1) % 2 == 0)
                {
                    ii = 2;
                }
                g.DrawString(checkCode.Substring(i, 1), f, b, 1 + (i * 12), ii);
            }
            //画一个边框
            g.DrawRectangle(new Pen(Color.Black, 0), 0, 0, image.Width - 1, image.Height - 1);

            //输出到浏览器
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                g.Dispose();
                image.Dispose();

                return ms.ToArray();
            }

        }
        #endregion

    }
}