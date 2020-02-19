using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QQLogin.Models;

namespace QQLogin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (!string.IsNullOrWhiteSpace(HttpContext.Request.Cookies["userinfo"]))
            {
                var ui = HttpContext.Request.Cookies["userinfo"];
                Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(ui);

                ViewBag.User = dic["nickname"];
                ViewBag.figureurl_qq_2 = dic["figureurl_qq_2"];
            }

           

            return View();
        }

        public const string appId = "101464898";//QQ的APPID
        public const string appSecret = "f460c6c3e3bcebd33d7c5a633006ff6f";//QQ的appSecret
        public const string redirecturl = "http://api.weqan.cn/Home/QQLogin";//回调地址

        /// <summary>
        /// 打开qq授权页面
        /// </summary>
        /// <returns></returns>
        public virtual IActionResult QqAuthorize()
        {
            var url = string.Format(
                     "https://graph.qq.com/oauth2.0/authorize?response_type=code&client_id={0}&redirect_uri={1}&state=State",
                     appId, WebUtility.UrlEncode(redirecturl));
            return new RedirectResult(url);
        }

        public virtual IActionResult QQLogin()
        {
            var code = Request.Query["code"];
            var token = GetAuthorityAccessToken(code);
            var dic = GetAuthorityOpendIdAndUnionId(token);
            var userInfo = GetUserInfo(token, dic["openid"]);

            //HttpContext.Session.SetString("userinfo", JsonConvert.SerializeObject(userInfo));



            HttpContext.Response.Cookies.Append("userinfo", JsonConvert.SerializeObject(userInfo));



            return RedirectToAction(nameof(Index));
        }



        public virtual string GetAuthorityAccessToken(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            try
            {
                var url =
                             string.Format(
                                 "https://graph.qq.com/oauth2.0/token?client_id={0}&client_secret={1}&code={2}&grant_type=authorization_code&redirect_uri={3}",
                                 appId, appSecret, code, redirecturl);

                HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;

                HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse;

                Stream stream = webResponse.GetResponseStream();

                using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    var json = reader.ReadToEnd();

                    if (string.IsNullOrEmpty(json))
                        return null;
                    if (!json.Contains("access_token"))
                    {
                        return null;
                    }

                    var dis = json.Split('&').Where(it => it.Contains("access_token")).FirstOrDefault();
                    var accessToken = dis.Split('=')[1];
                    return accessToken;

                }




            }
            catch (Exception)
            {

                return "";
            }




        }

        public virtual Dictionary<string, string> GetAuthorityOpendIdAndUnionId(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var url = $"https://graph.qq.com/oauth2.0/me?access_token={token}&unionid=1";
            HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;
            webRequest.Method = "GET";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse;

            Stream stream = webResponse.GetResponseStream();

            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                var json = reader.ReadToEnd();

                if (string.IsNullOrEmpty(json) || json.Contains("error") || !json.Contains("callback"))
                {
                    return null;
                }

                Regex reg = new Regex(@"\(([^)]*)\)");
                Match m = reg.Match(json);
                var dis = m.Result("$1");
                var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(dis);

                return dic;
            }





        }


        public virtual Dictionary<string, string> GetUserInfo(string token, string openId)
        {
            if (string.IsNullOrEmpty(token)) { return null; }

            var url = $"https://graph.qq.com/user/get_user_info?access_token={token}&openid={openId}&oauth_consumer_key={appId}";
            HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;

            HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse;
            Stream stream = webResponse.GetResponseStream();

            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                var json = reader.ReadToEnd();

                var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (dic.ContainsKey("ret") && dic["ret"] != "0")
                {
                    return null;
                }

                return dic;
            }






        }
    }
}
