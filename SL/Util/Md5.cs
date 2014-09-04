using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;

namespace INAnswer.Service
{
    public class Md5
    {
        #region md5加密码

        public static string Md516(string str)
        {
            using (System.Security.Cryptography.MD5CryptoServiceProvider oMD5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                return BitConverter.ToString(oMD5.ComputeHash(Encoding.UTF8.GetBytes(str)), 4, 8).Replace("- ", " ").ToLower();
            }
        }

        public static string MD5(string str)
        {
            return EncryptMD5ToHexString(str, System.Text.Encoding.UTF8.CodePage);
        }
        public static string EncryptMD5ToHexString(string s, int EncodingCodePage)
        {
            if (s == null || s == "") return null;
            return ByteArrayToHexString(EncryptMD5ToByteArray(s, EncodingCodePage));
        }
        public static string ByteArrayToHexString(byte[] ba)
        {
            string r = "";
            string s = "";
            for (long l = 0; l < ba.Length; l++)
            {
                byte b = (byte)(ba.GetValue(l));
                s = Convert.ToString(b, 16);
                if (s.Length < 2) s = "0" + s;
                r += s;
            }
            return r;
        }
        public static byte[] EncryptMD5ToByteArray(string s, int EncodingCodePage)
        {
            return EncryptMD5ToByteArray(StringToByteArray(s, EncodingCodePage));
        }
        public static byte[] EncryptMD5ToByteArray(byte[] ba)
        {
            using (System.Security.Cryptography.MD5CryptoServiceProvider oMD5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                byte[] r = oMD5.ComputeHash(ba);
                return r;
            }
        }
        public static byte[] StringToByteArray(string s, int EncodingCodePage)
        {
            return System.Text.Encoding.GetEncoding(EncodingCodePage).GetBytes(s);
        }
        #endregion

    }
}