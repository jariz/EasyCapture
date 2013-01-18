using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Threading;
using OAuth;
using System.Security.Cryptography;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EasyCapture
{
    class Credentials
    {
        #region external
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [DllImport("ole32.dll")]
        public static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        public static extern bool CredUnPackAuthenticationBuffer(int dwFlags, IntPtr pAuthBuffer, uint cbAuthBuffer, StringBuilder pszUserName, ref int pcchMaxUserName, StringBuilder pszDomainName, ref int pcchMaxDomainame, StringBuilder pszPassword, ref int pcchMaxPassword);

        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        public static extern int CredUIPromptForWindowsCredentials(ref CREDUI_INFO notUsedHere, int authError, ref uint authPackage, IntPtr InAuthBuffer, uint InAuthBufferSize, out IntPtr refOutAuthBuffer, out uint refOutAuthBufferSize, ref bool fSave, int flags);
        #endregion

        /*public static string AuthImgur()
        {
            var oauth = new Manager();
            oauth["consumer_key"] = Core.ApiKeys.Imgur.Key;
            oauth["consumer_secret"] = Core.ApiKeys.Imgur.Secret;
            oauth.AcquireRequestToken("https://api.imgur.com/oauth/request_token", "POST");
            ImgurAuth imgur = new ImgurAuth(new Uri("https://api.imgur.com/oauth/authorize?oauth_token=" + oauth["token"]));
            imgur.ShowDialog();
            printTokenInfo(oauth);
            if(imgur.pin != string.Empty)
                oauth.AcquireAccessToken("https://api.imgur.com/oauth/access_token", "GET", imgur.pin);
            printTokenInfo(oauth);
            return oauth["token"] + "|" + oauth["token_secret"];
        }*/

        public static string AuthImgur(int errorcode)
        {
            CREDUI_INFO credui = new CREDUI_INFO();
            credui.pszCaptionText = "Log in to imgur.com";
            credui.pszMessageText = "If you don't got a account, create one at imgur.com";
            credui.cbSize = Marshal.SizeOf(credui);
            uint authPackage = 0;
            IntPtr outCredBuffer = new IntPtr();
            uint outCredSize;
            bool save = false;

            int result = CredUIPromptForWindowsCredentials(ref credui, errorcode, ref authPackage, IntPtr.Zero, 0, out outCredBuffer, out outCredSize, ref save, 1);

            var usernameBuf = new StringBuilder(100);
            var passwordBuf = new StringBuilder(100);
            var domainBuf = new StringBuilder(100);

            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;
            if (result == 0)
            {
                if (CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName, domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
                {
                    //clear the memory allocated by CredUIPromptForWindowsCredentials 
                    CoTaskMemFree(outCredBuffer);
                    HttpWebRequest wc = (HttpWebRequest)HttpWebRequest.Create(new Uri("https://imgur.com/signin"));
                    wc.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11";
                    wc.Referer = "http://imgur.com";
                    
                    errorcode = 0;
                    NameValueCollection nvc = new NameValueCollection();
                    nvc.Add("username", usernameBuf.ToString());
                    nvc.Add("password", passwordBuf.ToString());
                    nvc.Add("remember", "remember");
                    nvc.Add("Submit", "Sign in");

                    ProgressDialog diag = new ProgressDialog(IntPtr.Zero);
                    diag.Line1 = "Authenticating with imgur...";
                    diag.Line2 = "EasyCapture is contacting the third party imgur server...";
                    diag.Line3 = " ";
                    diag.Title = "EasyCapture";
                    diag.Maximum = 1;
                    diag.CancelMessage = "Trying to stop....";
                    diag.ShowDialog();

                    diag.Value = 1337; //will create desired marquee effect

                    string resp = "";

                    HttpWebResponse web = null;
                    var parameters = new StringBuilder();
                    foreach (string key in nvc)
                    {
                        parameters.AppendFormat("{0}={1}&",
                            HttpUtility.UrlEncode(key),
                            HttpUtility.UrlEncode(nvc[key]));
                    }
                    parameters.Length -= 1;

                    wc.Method = "POST";
                    wc.ContentType = "application/x-www-form-urlencoded";
                    wc.AllowAutoRedirect = false;
                    

                    using (var writer = new StreamWriter(wc.GetRequestStream()))
                    {
                        writer.Write(parameters.ToString());
                    }

                    try
                    {
                        List<byte> buffer = new List<byte>();
                        web = (HttpWebResponse)wc.GetResponse();
                        Stream req = web.GetResponseStream();
                        while (true)
                        {
                            int bt = req.ReadByte();
                            if (bt == -1) break;
                            buffer.Add((byte)bt);
                        }
                        resp = Encoding.Default.GetString(buffer.ToArray());
                    }
                    catch (Exception z) { Out.WriteError(z.ToString()); resp = ""; }

                    
                            CookieContainer cc = new CookieContainer();
                            string gh = web.Headers["Set-Cookie"];
                            cc.SetCookies(new Uri("http://imgur.com"), web.Headers["Set-Cookie"]);

                    bool loc = true;
                    try
                    {
                        if (web.GetResponseHeader("Location") == string.Empty) throw new Exception("Woops");
                    }
                    catch
                    {
                        loc = false;
                    }
                    finally
                    {
                        if (loc)
                        {
                            HttpWebRequest web2 = (HttpWebRequest)HttpWebRequest.Create(new Uri(web.GetResponseHeader("Location")));
                            web2.Method = "GET";
                            web2.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11";
                            web2.Referer = "http://imgur.com";
                            web2.Headers.Set("Cookie", cc.GetCookieHeader(new Uri("http://imgur.com")));
                            try
                            {
                                Out.WriteDebug("Alternative cookie header: "+ ((HttpWebResponse)web2.GetResponse()).GetResponseHeader("Set-Cookie"));
                            }
                            catch
                            {
                                Out.WriteError("No new cookies");
                            }
                        }
                    }
                    diag.CloseDialog();

                    if (!diag.HasUserCancelled)
                    {

                        if (resp.Contains("Your login information was incorrect"))
                        {
                            errorcode = 1326;
                            return AuthImgur(errorcode);
                        }
                        else
                        {
                            TaskDialog.Show("Your imgur account has been successfully saved", "Authorization succeeded", "Yay!");

                            string header = cc.GetCookieHeader(new Uri("http://imgur.com"));
                            //wc.Headers.Add("Cookie", cc.GetCookieHeader(new Uri("http://imgur.com")));
                            //string aa = wc.DownloadString("http://api.imgur.com/2/account.json?_fake_status=200");
                            return Crypto.EncryptStringAES(header, Core.Secret);
                        }
                    }
                    else return "";
                }

                else errorcode = 1359; //an internal error occured
            }
            return "";
        }

        static void printTokenInfo(OAuth.Manager oauth)
        {
            Out.WriteDebug(string.Format("\r\ntoken: {0}\r\ntoken_secret: {1}", oauth["token"], oauth["token_secret"]));
        }

        public static string AuthPastebin(int errorcode)
        {
            CREDUI_INFO credui = new CREDUI_INFO();
            credui.pszCaptionText = "Log in to pastebin.com";
            credui.pszMessageText = "If you don't got a account, create one at pastebin.com";
            credui.cbSize = Marshal.SizeOf(credui);
            uint authPackage = 0;
            IntPtr outCredBuffer = new IntPtr();
            uint outCredSize;
            bool save = false;

            int result = CredUIPromptForWindowsCredentials(ref credui, errorcode, ref authPackage, IntPtr.Zero, 0, out outCredBuffer, out outCredSize, ref save, 1);

            var usernameBuf = new StringBuilder(100);
            var passwordBuf = new StringBuilder(100);
            var domainBuf = new StringBuilder(100);

            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;
            if (result == 0)
            {
                if (CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName, domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
                {
                    //clear the memory allocated by CredUIPromptForWindowsCredentials 
                    CoTaskMemFree(outCredBuffer);
                    using (WebClient wc = new WebClient())
                    {
                        errorcode = 0;
                        NameValueCollection nvc = new NameValueCollection();
                        nvc.Add("api_dev_key", Core.ApiKeys.Pastebin);
                        nvc.Add("api_user_name", usernameBuf.ToString());
                        nvc.Add("api_user_password", passwordBuf.ToString());

                        ProgressDialog diag = new ProgressDialog(IntPtr.Zero);
                        diag.Line1 = "Authenticating with pastebin...";
                        diag.Line2 = "EasyCapture is contacting the third party pastebin server...";
                        diag.Line3 = " ";
                        diag.Title = "EasyCapture";
                        diag.Maximum = 1;
                        diag.CancelMessage = "Trying to stop....";
                        diag.ShowDialog();

                        diag.Value = 1337; //will create desired marquee effect

                        string resp = "";

                        try
                        {
                            resp = Encoding.ASCII.GetString(wc.UploadValues(new Uri("http://pastebin.com/api/api_login.php"), nvc));
                        }
                        catch (Exception z) { Out.WriteError(z.ToString()); resp = "Bad API request, EC Network error"; }

                        
                        diag.CloseDialog();

                        if (!diag.HasUserCancelled)
                        {
                            

                            Out.WriteLine("Pastebin auth server returned: " + resp);
                            
                            if (resp.StartsWith("Bad API request"))
                            {
                                switch (resp)
                                {
                                    case "Bad API request, invalid login":
                                        errorcode = 1326;
                                        break;
                                    case "Bad API request, account not active":
                                        errorcode = 1331;
                                        break;
                                    default:
                                        errorcode = 59;
                                        break;
                                }

                                return AuthPastebin(errorcode);
                            }
                            else
                            {
                                TaskDialog.Show("Your pastebin account has been successfully saved", "Authorization succeeded", "Yay!");
                                return Crypto.EncryptStringAES(resp, Core.Secret);
                            }
                        }
                        else return "";
                    }
                }
                else errorcode = 1359; //an internal error occured
            }
            return "";
        }
        
        public static string[] AuthEasyCapture(int errorcode)
        {
            CREDUI_INFO credui = new CREDUI_INFO();
            credui.pszCaptionText = "Log in to EasyCaptu.re";
            credui.pszMessageText = "If you don't got a account, create one at EasyCaptu.re";
            credui.cbSize = Marshal.SizeOf(credui);
            uint authPackage = 0;
            IntPtr outCredBuffer = new IntPtr();
            uint outCredSize;
            bool save = false;

            int result = CredUIPromptForWindowsCredentials(ref credui, errorcode, ref authPackage, IntPtr.Zero, 0, out outCredBuffer, out outCredSize, ref save, 1);

            var usernameBuf = new StringBuilder(100);
            var passwordBuf = new StringBuilder(100);
            var domainBuf = new StringBuilder(100);

            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;
            if (result == 0)
            {
                if (CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName, domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
                {
                    //clear the memory allocated by CredUIPromptForWindowsCredentials 
                    CoTaskMemFree(outCredBuffer);
                    using (WebClient wc = new WebClient())
                    {
                        errorcode = 0;
                        
                        ProgressDialog diag = new ProgressDialog(IntPtr.Zero);
                        diag.Line1 = "Authenticating with EasyCaptu.re...";
                        diag.Line2 = "EasyCapture is contacting the EasyCapture server...";
                        diag.Line3 = " ";
                        diag.Title = "EasyCapture";
                        diag.Maximum = 1;
                        diag.CancelMessage = "Trying to stop....";
                        diag.ShowDialog();

                        diag.Value = 1337; //will create desired marquee effect

                        string resp = "";

                        try
                        {
                            resp = wc.DownloadString(new Uri(string.Format("http://api.easycaptu.re/login/{0}/{1}", usernameBuf.ToString(), passwordBuf.ToString()))); ;
                        }
                        catch (Exception z) { Out.WriteError(z.ToString()); resp = "Bad API request, EC Network error"; }

                        
                        diag.CloseDialog();

                        if (!diag.HasUserCancelled)
                        {
                            Out.WriteLine("EasyCapture auth server returned: " + resp);
                            JObject o = (JObject)JsonConvert.DeserializeObject(resp);
                            dynamic json = new JsonObject(o);

                            if (Convert.ToBoolean(json.error))
                            {
                                if (json.message == "Username/Password not found")
                                    return AuthEasyCapture(1326);
                                else return AuthEasyCapture(59);
                            }
                            else
                            {
                                TaskDialog.Show("Your EasyCaptu.re account has been successfully saved", "Authorization succeeded", "Yay!");
                                return new string[] { Crypto.EncryptStringAES(json.userkey, Core.Secret), json.username };
                            }
                        }
                        else return new string[] { "", "" };
                    }
                }
                else errorcode = 1359; //an internal error occured
            }
            return new string[] { "", "" };
        }
    
    }
}

