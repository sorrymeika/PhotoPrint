using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Functions
{
    public class Function
    {
        static sdk.WebService sms = new sdk.WebService();

        /// <summary>
        /// 普通群发短信mt方法
        /// </summary>
        /// <param name="sn">序列号</param>
        /// <param name="pwd">密码要MD5(SN+PWD)加密，取32位大写</param>
        /// <param name="mobiles">手机号列表</param>
        /// <param name="content">短信内容</param>
        /// <param name="ext">扩展码</param>
        /// <param name="stime">定时时间</param>
        /// <param name="rrid">流水号</param>
        /// <returns>发送结果</returns>
        public static string Mt(string sn, string pwd, string mobiles, string content, string ext, string stime, string rrid)
        {
            string result = sms.mt(sn, pwd, mobiles, content, ext, stime, rrid);
            return CheckResult(result);
        }


        public static string SendSMS(string sn, string pwd, string mobiles, string content)
        {
            string result = sms.SendSMS(sn, pwd, mobiles, content);
            return CheckResult(result);
        }

        /// <summary>
        /// 个性群发短信gxmt方法
        /// </summary>
        /// <param name="sn">序列号</param>
        /// <param name="pwd">密码要MD5(SN+PWD)加密，取32位大写</param>
        /// <param name="mobiles">手机号列表</param>
        /// <param name="content">短信内容,要经过URLgb2312编码和手机号对应好</param>
        /// <param name="ext">扩展码</param>
        /// <param name="stime">定时时间</param>
        /// <param name="rrid">流水号</param>
        /// <returns>发送结果</returns>
        public static string Gxmt(string sn, string pwd, string mobiles, string content, string ext, string stime, string rrid)
        {
            string result = sms.gxmt(sn, pwd, mobiles, content, ext, stime, rrid);
            return CheckResult(result);
        }

        /// <summary>
        /// 普通群发短信mdSmsSend_u方法
        /// </summary>
        /// <param name="sn">序列号</param>
        /// <param name="pwd">密码要MD5(SN+PWD)加密，取32位大写</param>
        /// <param name="mobiles">手机号列表</param>
        /// <param name="content">短信内容,要经过URLutf-8编码</param>
        /// <param name="ext">扩展码</param>
        /// <param name="stime">定时时间</param>
        /// <param name="rrid">流水号</param>
        /// <returns>发送结果</returns>
        public static string MdSmsSend_u(string sn, string pwd, string mobiles, string content, string ext, string stime, string rrid)
        {
            string result = sms.mdSmsSend_u(sn, pwd, mobiles, content, ext, stime, rrid);
            return CheckResult(result);
        }

        /// <summary>
        /// 查询短信剩余条数
        /// </summary>
        /// <param name="sn">序列号</param>
        /// <param name="pwd">密码要MD5(SN+PWD)加密，取32位大写</param>
        /// <returns>短信剩余条数</returns>
        public static string Balance(string sn, string pwd)
        {
            string result = sms.balance(sn, pwd);
            if (result.StartsWith("-"))
            {
                return "发送失败！" + GetWhy(result) + "。返回值是：" + result;
            }
            return "余额是：" + result;
        }


        /// <summary>
        /// 取回复短信
        /// </summary>
        /// <param name="sn">序列号</param>
        /// <param name="pwd">密码要MD5(SN+PWD)加密，取32位大写</param>
        /// <returns>回复短信</returns>
        public static string Mo(string sn, string pwd)
        {
            string result = sms.mo(sn, pwd);
            if (result.StartsWith("-"))
            {
                return "查询失败！返回值是：" + result;
            }

            if (result.Equals("1"))
            {
                return "当前没有用户上行短信";
            }

            StringBuilder sb = new StringBuilder();
            string[] temp = result.Split('\n');
            for (int i = 0; i < temp.Length; i++)
            {
                string[] detail = temp[i].Split(',');
                sb.AppendLine("第" + (i + 1).ToString() + "条");
                //sb.AppendLine("moID:"+detail[0]);
                //sb.AppendLine("特服号:" + detail[1]);
                sb.AppendLine("手机号:" + detail[2]);
                sb.AppendLine("回复内容:" + HttpUtility.UrlDecode(detail[3], Encoding.GetEncoding("gb2312")));//回复的内容需要解码);
                sb.AppendLine("回复时间:" + detail[4]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 给序列号充值
        /// </summary>
        /// <param name="sn">序列号</param>
        /// <param name="pwd">密码</param>
        /// <param name="CardNo">充值卡号</param>
        /// <param name="CardPwd">充值卡密码</param>
        /// <returns>充值结果</returns>
        public static string ChargeUp(string sn, string pwd, string CardNo, string CardPwd)
        {
            string result = sms.ChargUp(sn, pwd, CardNo, CardPwd);
            return result;
        }
        static string CheckResult(string result)
        {
            if (result.StartsWith("-"))
            {
                return "发送失败！" + GetWhy(result) + "。返回值是：" + result;
            }
            else
            {
                return "发送成功。流水号是：" + result;
            }
        }

        static string GetWhy(string code)
        {
            switch (code)
            {
                case "-2":
                    return "序列号未注册或加密不对";

                case "-4":
                    return "余额不足";
                case "-6":
                    return "参数错误，请检测所有参数";
                case "-7":

                    return "权限受限";

                case "-9":

                    return "扩展码权限错误";
                case "-10":

                    return "内容过长，短信不得超过500个字符";
                case "-20":

                    return "相同手机号，相同内容重复提交";
                case "-22":

                    return "Ip鉴权失败";

                default:
                    return "错误，请调试程序";
            }
        }
    }
}
