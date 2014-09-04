using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace INAnswer.Service
{
    public class RandomCode
    {
        private static readonly string[] allCharArray = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "S", "Y", "Z" };

        public static string i2s(int number)
        {
            int remainder = number % 36;
            string result = allCharArray[remainder];
            int a = number / 36;
            if (a >= 36)
            {
                result = i2s(a) + result;
            }
            else if (a > 0)
            {
                result = allCharArray[a] + result;
            }
            return result;
        }

        public static string MakeCoupon(int couponId, int couponLength)
        {
            return i2s(couponId) + Create(couponLength);
        }

        public static string Create(int codeCount)
        {
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

    }
}