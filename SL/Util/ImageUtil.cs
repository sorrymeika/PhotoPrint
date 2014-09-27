using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;

namespace SL.Util
{
    public class ImageUtil
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
            else if (mh == 0 || (sw / sh) > (mw / mh))
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


        public static Image GetThumbNailImage(Image originalImage, int thumMaxWidth, int thumMaxHeight)
        {
            Size thumRealSize = Size.Empty;
            Image newImage = originalImage;
            Graphics graphics = null;

            try
            {
                thumRealSize = getNewSize(thumMaxWidth, thumMaxHeight, originalImage.Width, originalImage.Height);
                newImage = new Bitmap(thumRealSize.Width, thumRealSize.Height);
                graphics = Graphics.FromImage(newImage);

                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Low;
                graphics.SmoothingMode = SmoothingMode.HighSpeed;

                graphics.Clear(Color.Transparent);

                graphics.DrawImage(originalImage, new Rectangle(0, 0, thumRealSize.Width, thumRealSize.Height), new Rectangle(0, 0, originalImage.Width, originalImage.Height), GraphicsUnit.Pixel);
            }
            catch
            {
            }
            finally
            {
                if (graphics != null)
                {
                    graphics.Dispose();
                    graphics = null;
                }
                originalImage.Dispose();
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

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        /// <summary>
        /// 图片压缩(降低质量以减小文件的大小)
        /// </summary>
        /// <param name="srcBitmap">传入的Bitmap对象</param>
        /// <param name="destStream">压缩后的Stream对象</param>
        /// <param name="level">压缩等级，0到100，0 最差质量，100 最佳</param>
        public static void Compress(Image srcBitmap, Stream destStream, int level)
        {
            ImageCodecInfo myImageCodecInfo;
            Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            // Get an ImageCodecInfo object that represents the JPEG codec.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Create an Encoder object based on the GUID

            // for the Quality parameter category.
            myEncoder = Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one

            // EncoderParameter object in the array.
            myEncoderParameters = new EncoderParameters(1);

            // Save the bitmap as a JPEG file with 给定的 quality level
            myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            srcBitmap.Save(destStream, myImageCodecInfo, myEncoderParameters);
        }

        public static byte[] Compress(Image srcBitmap, int level)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                Compress(srcBitmap, ms, level);
                return ms.ToArray();
            }
        }

        public static byte[] Compress(string fileName, int level, int thumMaxWidth, int thumMaxHeight)
        {
            Image image;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                image = GetThumbNailImage(System.Drawing.Image.FromStream(fs), thumMaxWidth, thumMaxHeight);
            }

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                Compress(image, ms, level);

                return ms.ToArray();
            }
        }

        public static byte[] Compress(Stream stream, int level, int thumMaxWidth, int thumMaxHeight)
        {
            Image image;
            using (stream)
            {
                image = GetThumbNailImage(System.Drawing.Image.FromStream(stream), thumMaxWidth, thumMaxHeight);
            }

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                Compress(image, ms, level);

                return ms.ToArray();
            }
        }

        public static byte[] Compress(string fileName, int maxWidth = 480)
        {
            return Compress(fileName, 40, maxWidth, 0);
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
            using (Bitmap image = new Bitmap(iwidth, 22))
            {
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
                    return ms.ToArray();
                }
            }
        }
        #endregion

    }

}