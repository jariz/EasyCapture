using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EasyCapture
{
    static class Core
    {
        public static string Version = "EasyCapture V" + Application.ProductVersion + " ALPHA";

        public static bool Online = true; // when set to false: causes all threads to exit

        public static bool Hook = false;
        public static bool Silent = false;

        private static bool textCapture = false;
        private static bool screenCapture = false;
        private static bool soundCapture = false;

        public static IniFile Settings;
        public static string UserDir = null;

        public static bool SettingsShowing = false;

        public static bool DebugMode = false;
        public static bool Imgur_FakeStatus = true;

        public static string Secret = "HsLqV25mEs1ibVd5";

        public static class ApiKeys
        {
            public static class Imgur
            {
                public static string Key = "102c9e0fe0bf576ac2e9a52aac047f22050810be1";
                public static string Secret = "44cbd7afe30b1ebdafbf02544bf32ca5";
                public static string DevKey = "7bac167ac9c123987754a24c06b82890";
            }
            public static string Pastebin = "97b1cac9bd8117db82e238492d58a99b";
        }

        public static string[] Reminders = { "You can change what happens after taking a capture under the 'Actions' tab", "You can add accounts for Imgur/Pastebin in the Account Management tab", "We're working on multi platform support, but currently only windows is supported.", "EasyCapture > Pastebin and Imgur", "Did you knew that EasyCapture was made by one guy? In 6 months?", "We try to give users as much customization, freedom and privacy as possible.", "We hate tracking, data-mining and ads. Check dontbubble.us for more information.", "There's gonna's be a lot of stuff the next few months" };


        private static string _status;
        public delegate void statusHandler(string status);
        public static event statusHandler statusChanged;

        public static string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                if (statusChanged != null) statusChanged(value);
            }
        }

        static void Refused()
        {
            TaskDialog task = new TaskDialog();
            task.InstructionText = "The EasyCapture server refused this file";
            task.Text = "The server might be down for maintenance, Please try again later.";
            task.Icon = TaskDialogStandardIcon.Error;
            task.Caption = "Upload failed";
            task.Show();
        }

        static void RequestCaptcha(string url, string code, string resp, CookieContainer cc)
        {
            using (WebClient wc = new WebClient())
            {
                NameValueCollection nvc = new NameValueCollection();
                PastebinCaptcha pc = new PastebinCaptcha(url.Replace("\"", ""), cc.GetCookieHeader(new Uri("http://pastebin.com")));
                string ch = cc.GetCookieHeader(new Uri("http://pastebin.com"));
                Out.WriteDebug("Cookie Header: " + ch);
                if (pc.ShowDialog() == DialogResult.Yes)
                {
                    nvc = new NameValueCollection();
                    nvc.Add("submit_hidden", "submit_hidden");
                    nvc.Add("captcha_item_key", code);
                    nvc.Add("security_code", pc.captcha);
                    nvc.Add("submit", "Submit");
                    wc.Headers.Add("Cookie", ch);
                    wc.Headers.Add("Referer", url.Replace("\"", ""));
                    wc.Headers.Add("Origin", "http://pastebin.com");
                    wc.Headers.Add("User-Agent", "EC RequestCaptcha " + Application.ProductVersion);
                    
                    string a = Encoding.Default.GetString(wc.UploadValues(resp, nvc));
                    if (a.Contains("'Wrong Captcha CODE, please try again.'"))
                    {
                        TaskDialog.Show("The code you entered was invalid according to pastebin, Please try again", "Invalid captcha");
                        RequestCaptcha(url, code, resp, cc);
                    }
                }
            }
        }

        public delegate void CaptureHandler();
        public static event CaptureHandler ScreenCaptureTrigger;
        public static event CaptureHandler TextCaptureTrigger;
        public static event CaptureHandler SoundCaptureTrigger;

        public static event CaptureHandler ScreenCaptureUp;
        public static event CaptureHandler TextCaptureUp;
        public static event CaptureHandler SoundCaptureUp;

        static TaskDialog Step2(TaskDialog d)
        {
            d.Text = "Do you want to follow a quick tutorial to get started with EasyCapture?";
            d.FooterText = "Question 2/2";
            TaskDialogCommandLink cmd1 = new TaskDialogCommandLink("cmd1", "Yes, I'll follow the - really short - tutorial", "Recommended for people who have never used EasyCapture before");
            TaskDialogCommandLink cmd2 = new TaskDialogCommandLink("cmd2", "No, I already know how the program works");
            cmd1.Click += delegate(object sender, EventArgs argz) { d.Close(); ThreadPool.QueueUserWorkItem(new WaitCallback(DemoThread)); };
            cmd2.Click += delegate(object sender, EventArgs argz) { d.Close(); };
            d.Controls.Add(cmd1);
            d.Controls.Add(cmd2);
            return d;
        }

        static TaskDialog WelcomeDialog()
        {
            TaskDialog d = new TaskDialog();
            d.InstructionText = "Welcome to EasyCapture";
            d.Text = "Do you want the program to start with windows?";
            d.Caption = "Welcome!";
            //d.FooterText = "Question 1/2";
            //d.FooterIcon = TaskDialogStandardIcon.Information;
            return d;
        }

        static void DemoThread(object b)
        {
            new Demo().ShowDialog();
        }

        public static event CaptureHandler STOPSS;

        static void GoSS(object st)
        {
            ss = new SoundScreen();
            ss.ShowDialog();
            
        }
        static SoundScreen ss;
        static void StopSS()
        {
            if (STOPSS != null) STOPSS();
        }


        public static bool isNewer(string currentversion, string serverversion)
        {
            try
            {
                string[] sversion = serverversion.Split(new string[] { "." }, StringSplitOptions.None);
                int index = -1;
                foreach (string a in currentversion.Split(new string[] { "." }, StringSplitOptions.None))
                {
                    index++;
                    int curr = Convert.ToInt32(a);
                    int ser = Convert.ToInt32(sversion[index]);

                    //current version's higher than server version (when i'm developing)
                    if (curr > ser)
                        return false;
                    //server version's higher than current version
                    if (ser > curr)
                        return true;
                }
                return false;
            }
            catch (Exception z)
            {
                Out.WriteError("Error parsing current/server version: " + z.ToString());
                return false;
            }
        }

        static void CleanUp()
        {
            foreach(string f in Directory.GetFiles(UserDir))
            {
                if(Path.GetFileName(f) != "EasyCapture.ini")
                    File.Delete(f);
            }
        }

        public static void openLink(string link)
        {
            Process.Start("explorer.exe", link);
        }
        
        static ProgressDialog diag;

        public static bool Ignore = false;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "/s":
                            Silent = true;
                            break;
                        case "/i":
                            Ignore = true;
                            break;
                    }
                }


           

            //Out.WriteBlank("^0C^1o^2l^3o^4r^5T^6e^7s^8t^9d^10A^11b^12c");
            //AllocConsole();
            //WINAPI.ShowWindow(WINAPI.GetConsoleWindow(), WINAPI.ShowWindowCommands.Hide);
            
            //WINAPI.ShowWindow(ConPtr, WINAPI.ShowWindowCommands.Hide);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Out.WritePlain(Core.Version);

            foreach (Process p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Application.ExecutablePath)))
            {
                if (p.MainModule.FileName == Application.ExecutablePath && !Ignore && p.Id != Process.GetCurrentProcess().Id)
                {
                    TaskDialog.Show("You cannot run multiple EasyCaptures, Please exit the currently running EasyCapture first.", "EasyCapture is already running");
                    Environment.Exit(1);
                }
            }

            if (Silent) Out.WriteLine("Running in silent mode. Not showing splash screen.");
            ServicePointManager.Expect100Continue = false;
            Core.Status = "Initializing user directory...";
            Out.WriteLine("Initializing user directory...");
            UserDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\EasyCapture";
            if (!Directory.Exists(UserDir)) Directory.CreateDirectory(UserDir);
            if (!File.Exists(UserDir + "\\EasyCapture.ini"))
                if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + "\\example.ini"))
                    File.Copy(Path.GetDirectoryName(Application.ExecutablePath) + "\\example.ini", UserDir + "\\EasyCapture.ini");
                else
                {
                    MessageBox.Show("EasyCapture could not find one if it's required files.\r\nPlease reinstall the program.", "EasyCapture installation corrupt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(10);
                }
            Core.Status = "Cleaning up user directory...";
            Out.WriteLine("Cleaning up user directory...");
            CleanUp();
            Core.Status = "Reading settings...";
            Out.WriteLine("Reading settings...");
            Core.Settings = new IniFile(UserDir + "\\EasyCapture.ini");
            if(!Silent)
                Silent = !Convert.ToBoolean(Convert.ToInt32(Settings.IniReadValue("MISC", "ShowSplash")));
            if (!Silent)
            {
                new Thread(splash).Start();
                while (statusChanged == null) { Thread.Sleep(250); }
            }
            if (Settings.IniReadValue("MISC", "Console") == "1")
            {
                startConsole();
                Console.Clear();
                Out.Write(Out.Buffer, true, new object[] { });
                Console.Title = "EasyCapture.Console";
            }
            if (Settings.IniReadValue("Update", "Disable") != "1")
            {
                Core.Status = "Checking for updates...";
                Out.WriteLine("Checking for updates...");
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        JObject o = (JObject)JsonConvert.DeserializeObject(wc.DownloadString("http://update.easycaptu.re/version"));
                        dynamic json = new JsonObject(o);
                        if (isNewer(Application.ProductVersion, json.version))
                        {
                            Out.WriteLine("Downloading installer...");
                            Status = "Downloading installer...";
                            string installer = UserDir + "\\EZCapInstaller-" + Path.GetRandomFileName().Replace(".", "") + ".exe";
                            new WebClient().DownloadFile("http://update.easycaptu.re/download/" + json.version + "/EasyCaptureInstaller.exe", installer);
                            System.Diagnostics.Process.Start(installer, "/f");
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                        }
                    }
                }
                catch (Exception z)
                {
                    Out.WriteError("Checking for updates failed: " + z);
                    Status = "Checking for updates failed :(";
                    Thread.Sleep(2000);
                }
            }
            Out.WriteLine("Setting up hotkey hook");
            new Thread(HotKeys.Init).Start();
            Core.Status = "Setting up audio driver...";
            Out.WriteLine("Setting up audio driver...");
            SoundCapture.Init();
            new Thread(icon).Start();

            if (Settings.IniReadValue("CHANGELOG", "ReadV" + Application.ProductVersion) != "1")
            {
                Settings.IniWriteValue("CHANGELOG", "ReadV" + Application.ProductVersion, "1");
                TaskDialog changelog = new TaskDialog();
                changelog.InstructionText = "EasyCapture has just been updated!";
                changelog.Text = "EasyCapture has just been updated, Please read through this changelog if you want to see what changes there are in this version";
                changelog.DetailsCollapsedLabel = "Show changelog";
                changelog.Caption = changelog.InstructionText;
                changelog.DetailsExpandedLabel = "Hide changelog";
                changelog.HyperlinksEnabled = true;
                changelog.HyperlinkClick += delegate(object sender, TaskDialogHyperlinkClickedEventArgs e)
                {
                    openLink(e.LinkText);
                };
                string chfile = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + Application.ProductVersion + ".txt";
                try
                {
                    changelog.DetailsExpandedText = File.ReadAllText(chfile);
                }
                catch
                {
                    changelog.DetailsExpandedText = string.Format("Unable to read changelog file '{0}'", chfile);
                }
                //changelog.DetailsExpanded = true;
                changelog.Icon = TaskDialogStandardIcon.Shield;
                changelog.Show();
            }

            //ThreadPool.QueueUserWorkItem(new WaitCallback(DemoThread));

            //Reminder("EC ALPHA RELEASE "+Application.ProductVersion, "Not for distribution, Testing build only.\r\nStill has - known - bugs.", 3000);

            //if (Settings.IniReadValue("MISC", "ShowLoginReminder") != "0")
                //Reminder("Create an account and get more!", "If you create a account you can see all your texts and images and modify them as well from any computer. Click this message to sign up!", 5000);

            if (Settings.IniReadValue("MISC", "NotFirstTime") != "1")
            {
                Settings.IniWriteValue("MISC", "NotFirstTime", "1");
                TaskDialog d = WelcomeDialog();
                TaskDialogCommandLink link1 = new TaskDialogCommandLink("link1", "Let EasyCapture start with windows");
                link1.ShowElevationIcon = true;
                TaskDialogCommandLink link2 = new TaskDialogCommandLink("link2", "Don't let EasyCapture start with windows", "If you change your mind, you can still change it in the settings");
                link1.Click += delegate(object sender, EventArgs arg) {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    key.SetValue("EasyCapture", "\"" + Application.ExecutablePath + "\" /s");
                    d.Close();
                    /*d = WelcomeDialog();
                    d = Step2(d);
                    d.Show();*/
                };
                link2.Click += delegate(object sender, EventArgs arg) {
                    d.Close();
                    /*d = WelcomeDialog();
                    d = Step2(d);
                    d.Show();*/
                };
                d.Controls.Add(link1);
                d.Controls.Add(link2);
                d.Show();



                CustomBalloon("Oh, Hai there!", "You can click this icon to customize EasyCapture to your likings!", 5000, ECIcon.BigIcon);

                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o) { Thread.Sleep(6000);
                    Keys textkeys = (Keys)Convert.ToInt32(Core.Settings.IniReadValue("HotKeys", "text"));
                    Keys screenkeys = (Keys)Convert.ToInt32(Core.Settings.IniReadValue("HotKeys", "screen"));
                    Keys soundkeys = (Keys)Convert.ToInt32(Core.Settings.IniReadValue("HotKeys", "sound"));

                    CustomBalloon("Wondering how to take a capture?",
                        "Press " + SettingsV3.getModifier(screenkeys).ToString() + "+" + SettingsV3.removeModifiers(screenkeys).ToString() + " to take a Screen Capture\r\n" +
                        "Press " + SettingsV3.getModifier(textkeys).ToString() + "+" + SettingsV3.removeModifiers(textkeys).ToString() + " to take a Text Capture\r\n" +
                        "Press " + SettingsV3.getModifier(soundkeys).ToString() + "+" + SettingsV3.removeModifiers(soundkeys).ToString() + " to take a Sound Capture\r\n"

                   , 10000, ECIcon.BigIcon);
                
                }), null);
                

            }


            int garbagecollects = 0;
            int pointer = 0;
            Out.WriteDebug("Hotkey loop started");
            while (Online)
            {
                try
                {
                    Thread.Sleep(100);

                    // RAM garbage collecting
                    pointer++;
                    if (pointer == 100)
                    {
                        GC.Collect();
                        garbagecollects++;
                        pointer = 0;
                    }

                    if (soundCapture)
                    {
                        if (SoundCaptureUp != null) SoundCaptureUp();
                        //if (!SoundCapture.Recording) Reminder("Recording sound...", "EasyCapture is now recording all sound output from the sound device '" + SoundCapture.RecordDevice + "'. Press the SoundCapture button again to cancel. There is a 30 seconds limit.", 5000);
                        ThreadPool.QueueUserWorkItem(GoSS);

                        soundCapture = false;
                        SoundCapture.Record();
                        int i = 0;
                        while (SoundCapture.Recording && soundCapture == false && i != 300)
                        {
                            Thread.Sleep(100);
                            i++;
                        }
                        soundCapture = false;
                        var file = SoundCapture.Stop();
                        StopSS();
                        //upload
                        ThreadPool.QueueUserWorkItem(new WaitCallback(doDiag));

                        WebClient wc = new WebClient();
                        NameValueCollection nvc = new NameValueCollection();
                        nvc.Add("type", "sound");
                        nvc.Add("content", Convert.ToBase64String(File.ReadAllBytes(file)));
                        File.Delete(file);
                        bool w8 = true;
                        bool canceled = false;
                        string resp = "";
                        wc.UploadProgressChanged += delegate(object sender, UploadProgressChangedEventArgs e)
                        {
                            diag.Line3 = e.BytesSent + "/" + e.TotalBytesToSend;
                            diag.Value = Convert.ToUInt32(e.ProgressPercentage);
                            if (diag.HasUserCancelled)
                            {
                                wc.CancelAsync();
                                canceled = true;
                            }
                        };
                        wc.UploadValuesCompleted += delegate(object sender, UploadValuesCompletedEventArgs e)
                        {
                            if (e.Error != null && !canceled)
                                Refused();
                            else
                            {
                                if (!canceled)
                                {
                                    Out.WriteLine("Sound uploaded");
                                    resp = Encoding.Default.GetString(e.Result);
                                }
                                diag.CloseDialog();
                                w8 = false;
                            }

                        };
                        string key = Core.Settings.IniReadValue("MISC", "userkey");
                        string ukey = "";
                        if (key != string.Empty)
                            ukey = "?userkey=" + Crypto.DecryptStringAES(key, Core.Secret);
                        wc.UploadValuesAsync(new Uri("http://upload.easycaptu.re/" + Application.ProductVersion+ukey), nvc);
                        /*try
                        {
                            resp = Encoding.Default.GetString(wc.UploadValues(new Uri("http://upload.easycaptu.re/" + Application.ProductVersion), nvc));
                        }
                        catch
                        {

                        }*/
                        while (w8)
                        {
                            
                            Thread.Sleep(100);
                        }

                        if (!canceled)
                        {
                            Out.WriteLine("Server responded:\r\n" + resp);
                            List<EasyCaptureResponse> keys = (List<EasyCaptureResponse>)JsonConvert.DeserializeObject(resp, typeof(List<EasyCaptureResponse>));
                            if (keys != null || resp != string.Empty)
                            {
                                var inn = String.Format("http://in.easycaptu.re/{0}/{1}", keys[0].hash, keys[0].authcode);
                                int action = Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "sound"));
                                Out.WriteDebug("action=" + action);
                                if (action == 0)
                                {
                                    Out.WriteLine("Opening '" + inn + "'");
                                    openLink(inn);
                                }
                                else if (action == 1)
                                {
                                    string raw = "http://easycaptu.re/" + keys[0].hash + ".mp3";
                                    Out.WriteLine("Opening '" + raw + "'");
                                    openLink(raw);
                                }
                                else if (action == 2)
                                {
                                    string copy = "http://easycaptu.re/" + keys[0].hash;
                                    Clipboard.SetText(copy);
                                    Out.WriteLine("Copied '" + copy + "' to clipboard");
                                    Reminder("Link copied", "Link was copied to your clipboard", 2000);
                                }
                            }
                            else
                                Refused();
                        }
                        if (SoundCaptureTrigger != null) SoundCaptureTrigger();
                        soundCapture = false;
                    }
                    if (textCapture)
                    {
                        if (TextCaptureUp != null) TextCaptureUp();
                        string ccopy = HotKeys.Copy();
                        if (ccopy == string.Empty)
                        {
                            TaskDialog err = new TaskDialog();
                            err.Icon = TaskDialogStandardIcon.Error;
                            err.Text = "Are you sure you selected something?";
                            err.InstructionText = "Unable to get selected text";
                            err.Show();
                        }
                        else
                        {
                            if (Settings.IniReadValue("TextCapture", "pastebin") != "1")
                            {
                                WebClient wc = new WebClient();
                                NameValueCollection nvc = new NameValueCollection();
                                nvc.Add("type", "text");
                                nvc.Add("content", Convert.ToBase64String(Encoding.Default.GetBytes(ccopy)));
                                //Out.WriteDebug("[DEBUG] [SUPERDEBUG] [EXPIREMENTAL] " + nvc["content"]);
                                string resp = "";
                                string key = Core.Settings.IniReadValue("MISC", "userkey");
                                string ukey = "";
                                if (key != string.Empty)
                                    ukey = "?userkey=" + Crypto.DecryptStringAES(key, Core.Secret);
                                resp = Encoding.Default.GetString(wc.UploadValues("http://upload.easycaptu.re/" + Application.ProductVersion + ukey, "POST", nvc));
                                Out.WriteLine("Server responded:\r\n" + resp);

                                List<EasyCaptureResponse> keys = (List<EasyCaptureResponse>)JsonConvert.DeserializeObject(resp, typeof(List<EasyCaptureResponse>));
                                if (keys != null || resp != string.Empty)
                                {
                                    var inn = String.Format("http://in.easycaptu.re/{0}/{1}", keys[0].hash, keys[0].authcode);
                                    int action = Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "text"));
                                    Out.WriteDebug("action=" + action);
                                    if (action == 0)
                                    {
                                        Out.WriteLine("Opening '" + inn + "'");
                                        openLink(inn);
                                    }
                                    else if (action == 1)
                                    {
                                        string raw = "http://easycaptu.re/" + keys[0].hash + ".txt";
                                        Out.WriteLine("Opening '" + raw + "'");
                                        openLink(raw);
                                    }
                                    else if (action == 2)
                                    {
                                        string copy = "http://easycaptu.re/" + keys[0].hash;
                                        Clipboard.SetText(copy);
                                        Out.WriteLine("Copied '" + copy + "' to clipboard");
                                        Reminder("Link copied", "Link was copied to your clipboard", 2000);
                                    }
                                    else
                                        Refused();
                                }
                            }
                            else
                            {
                                using (WebClient wc = new WebClient())
                                {
                                    string user = Crypto.DecryptStringAES(Settings.IniReadValue("TextCapture", "user_key"), Core.Secret);
                                    NameValueCollection nvc = new NameValueCollection();
                                    nvc.Add("api_dev_key", ApiKeys.Pastebin);
                                    nvc.Add("api_option", "paste");
                                    nvc.Add("api_paste_code", ccopy);

                                    nvc.Add("api_paste_private", Settings.IniReadValue("TextCapture", "exposure"));
                                    nvc.Add("api_paste_name", "Uploaded with easycaptu.re - Share media instantly!");
                                    if (user != string.Empty) nvc.Add("api_user_key", user);
                                    string resp = Encoding.Default.GetString(wc.UploadValues("http://pastebin.com/api/api_post.php", nvc));
                                    Out.WriteDebug("Server responded:\r\n" + resp);
                                    //if(!resp.StartsWith("http://    
                                    Out.WriteLine("Checking for captcha's....");
                                    wc.Headers.Add("User-Agent", "EC RequestCaptcha " + Application.ProductVersion);
                                    string spam = wc.DownloadString(resp);
                                    CookieContainer cc = new CookieContainer();
                                    cc.SetCookies(new Uri("http://pastebin.com"), wc.ResponseHeaders["Set-Cookie"]);
                                    string url = "";
                                    string code = "";
                                    foreach (string a in spam.Split(new string[] { "\n" }, StringSplitOptions.None))
                                    {
                                        if (a.Contains("<img id=\"siimage\""))
                                        {
                                            string re1 = ".*?";	// Non-greedy match on filler
                                            string re2 = "\".*?\"";	// Uninteresting: string
                                            string re3 = ".*?";	// Non-greedy match on filler
                                            string re4 = "\".*?\"";	// Uninteresting: string
                                            string re5 = ".*?";	// Non-greedy match on filler
                                            string re6 = "(\"/etc.*?\")";	// Double Quote String 1

                                            Regex r = new Regex(re1 + re2 + re3 + re4 + re5 + re6, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                            Match m = r.Match(a);
                                            if (m.Success)
                                            {
                                                url = m.Groups[1].ToString();
                                            }
                                        }
                                        if (a.Contains("captcha_item_key"))
                                        {
                                            string re1 = ".*?";	// Non-greedy match on filler
                                            string re2 = "(?:[a-z][a-z0-9_]*)";	// Uninteresting: var
                                            string re3 = ".*?";	// Non-greedy match on filler
                                            string re4 = "(?:[a-z][a-z0-9_]*)";	// Uninteresting: var
                                            string re5 = ".*?";	// Non-greedy match on filler
                                            string re6 = "(?:[a-z][a-z0-9_]*)";	// Uninteresting: var
                                            string re7 = ".*?";	// Non-greedy match on filler
                                            string re8 = "(?:[a-z][a-z0-9_]*)";	// Uninteresting: var
                                            string re9 = ".*?";	// Non-greedy match on filler
                                            string re10 = "((?:[a-z][a-z0-9_]*))";	// Variable Name 1

                                            Regex r = new Regex(re1 + re2 + re3 + re4 + re5 + re6 + re7 + re8 + re9 + re10, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                            Match m = r.Match(a);
                                            if (m.Success)
                                            {
                                                code = m.Groups[1].ToString();
                                            }
                                        }
                                    }

                                    Out.WriteDebug("---- headerdump ----");
                                    foreach (string d in wc.Headers)
                                    {
                                        Out.WriteDebug(d);
                                    }

                                    Out.WriteDebug("Image = " + url + " Captcha key = " + code);
                                    if (url != string.Empty)
                                    {
                                        RequestCaptcha(url, code, resp, cc);
                                    }

                                    switch (Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "text")))
                                    {
                                        case 0:
                                            Out.WriteLine("Opening " + resp);
                                            openLink(resp);
                                            break;
                                        case 1:
                                            string dd = "http://pastebin.com/raw.php?i=" + resp.Replace("http://pastebin.com/", "");
                                            Out.WriteLine("Opening " + dd);
                                            openLink(resp);
                                            break;
                                        case 2:
                                            Out.WriteLine("Copying " + resp);
                                            Clipboard.SetText(resp);
                                            Out.WriteLine("Copied '" + resp + "' to clipboard");
                                            Reminder("Link copied", "Link was copied to your clipboard", 2000);
                                            break;
                                    }
                                }
                            }
                        }

                        if (TextCaptureTrigger != null) TextCaptureTrigger();
                        textCapture = false;
                    }
                    if (screenCapture)
                    {
                        if (ScreenCaptureUp != null) ScreenCaptureUp();
                        Form1 screencap = new Form1(false);
                        screencap.ShowDialog();
                        if (!screencap.abort)
                        {
                            string resp = Encoding.Default.GetString(screencap.DATAZ);
                            Out.WriteLine("Server responded:\r\n" + resp);
                            if (!screencap.imgur)
                            {
                                List<EasyCaptureImageResponse> keys = (List<EasyCaptureImageResponse>)JsonConvert.DeserializeObject(resp, typeof(List<EasyCaptureImageResponse>));

                                if (keys != null || resp != string.Empty)
                                {
                                    int action = Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "screen"));
                                    Out.WriteDebug("action=" + action);
                                    var inn = String.Format("http://in.easycaptu.re/{0}/{1}", keys[0].hash, keys[0].authcode);
                                    if (action == 0)
                                    {
                                        Out.WriteLine("Opening " + inn);
                                        openLink(inn);
                                    }
                                    else if (action == 1)
                                    {
                                        Out.WriteLine("Opening absolute pic");
                                        openLink(string.Format("http://easycaptu.re/{0}.png", keys[0].hash));
                                    }
                                    else if (action == 2)
                                    {
                                        Out.WriteLine("Copying link");
                                        //Invoke(new cliphandler(Clipboard.SetText), (object)(string.Format("http://easycapture.re/{0}", keys[0].hash)));
                                        Clipboard.SetText(string.Format("http://easycaptu.re/{0}", keys[0].hash));
                                        Reminder("Link copied", "Link was copied to your clipboard", 2000);
                                    }
                                }
                                else
                                    Refused();
                            }
                            else
                            {
                                try
                                {
                                    XmlDocument doc = new XmlDocument();
                                    doc.LoadXml(resp);
                                    int action = Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "screen"));
                                    string url = doc.GetElementsByTagName("imgur_page")[0].InnerText;
                                    string direct = doc.GetElementsByTagName("original")[0].InnerText;
                                    switch (action)
                                    {
                                        case 0:
                                            Out.WriteLine("Opening " + url);
                                            openLink(url);
                                            break;
                                        case 1:
                                            Out.WriteLine("Opening absolute pic");
                                            openLink(direct);
                                            break;
                                        case 2:
                                            Out.WriteLine("Copying link");
                                            Clipboard.SetText(url);
                                            Reminder("Link copied", "Link was copied to your clipboard", 2000);
                                            break;
                                        default:
                                            TaskDialog.Show("Invalid action (see %AppData%\\EasyCapture.ini)");
                                            break;
                                    }
                                }
                                catch(Exception z)
                                {
                                    Out.WriteError("Error while parsing response: " + z.ToString());
                                    Refused();
                                }
                            }
                            
                        }

                        if (ScreenCaptureTrigger != null) ScreenCaptureTrigger();
                        screenCapture = false;
                    }
                }
                catch (Exception z) {
                    //if (!DebugMode) 
                        FatalError(z);
                    //else throw (z);
                }
            }
            Out.WriteLine("Main thread stopped, all other threads are offline as well.");
            Out.WriteDebug("BYE!");
            //Thread.Sleep(2000);
            //Application.Run(new Form1());
        }

        static void doDiag(object b)
        {
            diag = new ProgressDialog(IntPtr.Zero);
            diag.Line1 = "Uploading sound to the cloud...";
            diag.Line2 = "Uploading raw sound to the cloud...";
            diag.Line3 = "Connecting... ";
            diag.CancelMessage = "Trying to abort upload";
            diag.Title = "EasyCapture";
            diag.ShowDialog();
        }

        static void wc_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            try
            {
                
                //Out.WriteLine(e.ProgressPercentage + " " + e.BytesSent + "/" + e.TotalBytesToSend);
            }
            catch
            {
            }
        }

        static void splash()
        {
            Splash s = new Splash();
            s.ShowDialog();
        }

        delegate void ReminderCallback();
        static event ReminderCallback ReminderClick;

        public static void Reminder(string instruction, string text, int time)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(iReminder), (object)new string[] { instruction, text, time.ToString() });
        }

        static void iReminder(object args)
        {
            string[] argz = (string[])args;
            var rem = new Reminder(argz[0], argz[1], Convert.ToInt32(argz[2]));
            rem.Click += delegate(object sender, EventArgs e) { if (ReminderClick != null) ReminderClick(); };
            rem.ShowDialog();
        }

        public static void doCapture()
        {
            screenCapture = true;
        }

        public static void doSound()
        {
            soundCapture = true;
        }

        public static void doText()
        {
            textCapture = true;
        }
        public static NotifyIcon Icon;
        static void icon()
        {
            Status = "Setting up tray";
            Out.WriteLine("Icon thread started");
            Icon = new NotifyIcon();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            Icon.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            Icon.Visible = true;
            Icon.Text = Core.Version;
            Icon.MouseClick += delegate(object sender, MouseEventArgs args) {
                if (args.Button == MouseButtons.Left)
                {
                    Thread a = new Thread(SettingsThread);
                    a.SetApartmentState(ApartmentState.STA);
                    a.Start();
                }
            };

            Icon.BalloonTipClicked += delegate(object sender, EventArgs args)
            {
                Thread a = new Thread(SettingsThread);
                a.SetApartmentState(ApartmentState.STA);
                a.Start();
            };
            
            ContextMenuStrip context = new ContextMenuStrip();
            context.Items.Add("Settings");
            context.Items[0].Click += new EventHandler(Core_Click);
            context.Items[0].Image = Properties.Resources.cog;
            context.Items.Add("About");
            context.Items[1].Click += new EventHandler(Core_Click2);
            context.Items[1].Image = Properties.Resources.emoticon_waii;
            context.Items.Add("Donate");
            context.Items[2].Image = Properties.Resources.money_dollar;
            context.Items[2].Click += delegate(object s, EventArgs e) { Core.openLink("http://easycaptu.re/donate"); };
            context.Items.Add("Exit");
            context.Items[3].Click +=new EventHandler(Core_Click1);
            context.Items[3].Image = Properties.Resources.cross;
            //context.Items.Add("Login with EasyCapture account");
            //context.Items[2].Click +=new EventHandler(Login);
            if (DebugMode)
            {
                context.Items.Add("Hide console");
                context.Items[2].Click +=new EventHandler(DebugConsole);
                context.Items.Add("[DEBUG] Stop recording manually");
                context.Items[3].Click +=new EventHandler(Core_Click11);
            }
            context.Click += new EventHandler(context_Click);
            //context.Show();
            Icon.ContextMenuStrip = context;
            //context.Show();
            context.Enabled = true;
            Out.WriteLine("Icon online");
            Status = "Waiting for input";
            //new Settings().ShowDialog();


            Application.Run();

            

            Online = false;
        }

        enum ECIcon { SmallIcon, BigIcon };

        static void CustomBalloon(string title, string content, int timeout, ECIcon icon)
        {
            IntPtr nv = ((NativeWindow)Icon.GetType().GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance).GetValue(Icon)).Handle;
            WINAPI.NOTIFYICONDATA pnid = new WINAPI.NOTIFYICONDATA();
            pnid.hWnd = nv;
            pnid.uID = 1;
            pnid.uFlags = 16;
            pnid.uTimeoutOrVersion = timeout;
            pnid.szInfoTitle = title;
            pnid.szInfo = content;
            pnid.dwInfoFlags = 0x00000004; //NIIF_USER
            switch (icon)
            {
                case ECIcon.SmallIcon:
                    pnid.hIcon = Properties.Resources.ec3_32.Handle;
                    break;
                case ECIcon.BigIcon:
                    pnid.hIcon = Properties.Resources.ec3_64.Handle;
                    break;
            }
            if (WINAPI.Shell_NotifyIcon(1, pnid) == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        static void aboutTh()
        {
            new About().ShowDialog();
        }

        static void Core_Click2(object sender, EventArgs e)
        {
            Thread th = new Thread(aboutTh);
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        static void Core_Click1(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        static void context_Click(object sender, EventArgs e)
        {
            //new Settings().Show();
        }

        static void Core_Click11(object sender, EventArgs e)
        {
            soundCapture = true;
        }

        [MTAThread]
        static void SettingsThread(object zz)
        {
            if (!SettingsShowing)
            {
                SettingsShowing = true;
                SettingsV3 v3 = null;
                try
                {
                    v3 = new SettingsV3();
                }
                catch (Exception z)
                {
                    TaskDialog d = new TaskDialog();
                    d.InstructionText = "Unable to load settings";
                    d.Text = "The configuration file might be corrupt, do you want to reset it?";
                    TaskDialogCommandLink l1 = new TaskDialogCommandLink("l1", "Reset the configuration file", "This will delete your current settings, but then you'll be able to open the settings again");
                    TaskDialogCommandLink l2 = new TaskDialogCommandLink("l2", "Don't reset the configuration file", "Settings screen won't load anymore, but you'll keep your settings");
                    l1.Click += delegate(object a, EventArgs b) { File.Delete(Core.Settings.path); File.Copy(Path.GetDirectoryName(Application.ExecutablePath) + "\\example.ini", Core.Settings.path); Core_Click(null, null); };
                    l2.Click += delegate(object a, EventArgs b) { d.Close(); };
                    d.Controls.Add(l1);
                    d.Controls.Add(l2);
                    d.Icon = TaskDialogStandardIcon.Error;
                    d.Caption = d.InstructionText;
                    d.DetailsCollapsedLabel = "Show error";
                    d.DetailsExpandedLabel = "Hide error";
                    d.DetailsExpandedText = z.ToString();
                    d.Show();
                }
                finally
                {
                    try
                    {
                        if (v3 != null)
                        {
                            v3.ShowDialog();
                            v3.Dispose();
                            GC.Collect();
                        }
                    }
                    catch (Exception z)
                    {
                        FatalError(z);
                    }
                }
                SettingsShowing = false;
            }
        }

        static void Core_Click(object sender, EventArgs e)
        {
            Thread a = new Thread(SettingsThread);
            a.SetApartmentState(ApartmentState.STA);
            a.Start();
        }

        static void item_Click(object sender, EventArgs e)
        {
            
        }

        public static bool ConVisible = true;
        public static IntPtr ConPtr;

        public static void DebugConsole(object a, EventArgs b)
        {
            if(ConPtr == IntPtr.Zero) ConPtr = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (ConVisible)
            {
                Icon.ContextMenuStrip.Items[2].Text = "Show console";
                WINAPI.ShowWindow(ConPtr, WINAPI.ShowWindowCommands.Hide);
                ConVisible = false;
            }
            else
            {
                Icon.ContextMenuStrip.Items[2].Text = "Hide console";
                ConVisible = true;
                WINAPI.ShowWindow(ConPtr, WINAPI.ShowWindowCommands.Restore);
            }
        }

        public static void FatalError(Exception z)
        {
            Out.WritePlain("A unhandled exception was acquired. Below is the information (possibly repeated) of the exception which made EZCap crash.");
            Out.WriteError("\r\n----------- EZCAP FATALERROR -----------\r\n" + z.ToString() + "\r\n----------------------------------------");
            TaskDialog diag = new TaskDialog();
            diag.InstructionText = "EasyCapture has failed you, master.";
            diag.Text = "EasyCapture has encountered an fatal error.";
            diag.Icon = TaskDialogStandardIcon.Error;
            diag.DetailsExpandedText = z.ToString();
            TaskDialogCommandLink cmd1 = new TaskDialogCommandLink("exit", "Exit the application");
            TaskDialogCommandLink cmd2 = new TaskDialogCommandLink("continue", "Continue and ignore error");
            TaskDialogCommandLink cmd3 = new TaskDialogCommandLink("debug", "Debug the program", "Show the console and try to find out what happend");
            cmd1.Click += delegate(object a, EventArgs b) { Environment.Exit(1); };
            cmd2.Click += delegate(object a, EventArgs b) { diag.Close(); };
            cmd3.Click += delegate(object a, EventArgs b) { 
                diag.Close();
                startConsole();
                Out.WritePlain("EZCap debug mode entered");
                Out.WritePlain("EZCap was forced to stop executing. Press enter to resume.");
                Console.Title = "EasyCapture Debug Session";
                Console.ReadLine();
                WINAPI.FreeConsole();
            };
            diag.Controls.Add(cmd1);
            diag.Controls.Add(cmd2);
            diag.Controls.Add(cmd3);
            diag.Caption = "Oops!";
            diag.DetailsExpandedLabel = "Hide error";
            diag.DetailsCollapsedLabel = "Show error";
            diag.Show();
        }

        public static void startConsole()
        {
            WINAPI.FreeConsole();
            WINAPI.AllocConsole();
            WINAPI.RECT rect;
            WINAPI.GetWindowRect(WINAPI.GetConsoleWindow(), out rect);
            WINAPI.SetWindowPos(WINAPI.GetConsoleWindow(), IntPtr.Zero, 0, 0, rect.Width, rect.Height, 0x0040);
            Console.WindowHeight = Console.LargestWindowHeight;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
            Out.Write(Out.Buffer, true, new object[] { });
        }
    }
    public class EasyCaptureResponse
    {
        public string hash { get; set; }
        public string authcode { get; set; }
    }
}
