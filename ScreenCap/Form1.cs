using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Xml;

namespace EasyCapture
{
    public partial class Form1 : Form
    {

        Color COLAHR = Color.LightSkyBlue;
        
        public Form1(bool pauseoverride)
        {
            InitializeComponent();

            this.pauseoverride = pauseoverride;
        }

        bool pauseoverride = false;

        public bool pause = false;
        public bool imgur = false;

        private void Form1_Load(object sender, EventArgs e)
        {
            if(!pauseoverride) pause = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("ScreenCapture", "freeze")));
            imgur = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("ScreenCapture", "imgur")));

            //WARNING: this code was written while I was drunk!!!!!!!!!
            //edit: and has been rewritten numerous times since then
            /*Out.WriteLine("---- MONITORS ----");
            Out.WriteDebug("Using X:0 Y:0 as default location");
            Rectangle primary = Screen.PrimaryScreen.Bounds;
            */
            /*Screen.

            foreach (Screen screen in Screen.AllScreens)
            {
                if(!screen.Primary)
                    primary.Inflate(screen.Bounds.Width, 0);
            }

            Location = new Point(0, 0);
            Size = new Size(w, h);

            Out.WriteLine("---- Calculation result ----");
            Out.WriteLine(Size.ToString());
            Out.WriteLine(Location.ToString());
            Out.WriteLine("----------------------------");*/

            var rect = SystemInformation.VirtualScreen;
            Size = new Size(rect.Width, rect.Height);
            Location = new Point(rect.Left, rect.Top);

            if (pause)
            {

                Out.WriteLine("NOW FREEEEZEEEEEE");
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(this.Width, this.Height);
                gfxScreenshot = Graphics.FromImage(bmp);
                Opacity = 0.0;
                gfxScreenshot.CopyFromScreen(0, 0, 0, 0, this.Size, CopyPixelOperation.SourceCopy);
                panel1.Hide();
                this.BackgroundImage = bmp;

                Cursor = Cursors.WaitCursor;
                
                timer1.Start();
            }

        }

        Point loc;
        bool down = false;
        private void panel1_MouseDown(object sender, MouseEventArgs e) /* actually is form. gooby pls. k. */
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.Size = new Size(100, 100);
                this.Location = MousePosition;
                Opacity = 0.5;
                foreach (Control c in Controls)
                {
                    c.BackColor = COLAHR;
                }
                BackColor = COLAHR;
                loc = MousePosition;
                down = true;
            }
            else
            {
                abort = true;
                Close();
            }
        }

        Point p;
        bool pos = false;
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (down)
            {
                if ((MousePosition.X < this.Location.X) && (MousePosition.Y < this.Location.Y))
                {
                    if (!pos)
                    {
                        pos = true;
                        p = MousePosition;
                    }
                    this.Size = new Size(p.X - MousePosition.X, p.Y - MousePosition.Y);
                    this.Location = MousePosition;
                }
                else
                {
                    pos = false;
                    this.Size = new Size(MousePosition.X - this.Location.X + 1, MousePosition.Y - this.Location.Y + 1);
                }
                label1.Text = Size.Width + "\r\n" + Size.Height;

            }
        }

        int progress = 0;
        int bytes = 0;
        int totalbytes = 0;
        bool done = false;
        int waitz = 0;
        bool cancel = false;

        public bool abort = false;

        WebClient wc;

        ProgressDialog diag;

        delegate void ECHandler();
        [STAThread]
        void checkProgress(object arg)
        {
            while (!done)
            {
                if (waitz == 10)
                {
                    //1 SECOND?! ACTIVATE PROGRESS DIALOG!
                    diag = new ProgressDialog(IntPtr.Zero);
                    diag.Line1 = "Uploading your picture...";
                    diag.Line2 = "Uploading your picture takes longer than expected....";
                    diag.Title = "EasyCapture";
                    diag.Maximum = 100;
                    diag.CancelMessage = "Trying to stop....";
                    diag.ShowDialog();
                    Out.WriteLine("Uploading takes longer then 1 second. Activating progress dialog");
                }

                Out.WriteDebug(string.Format("checkProgress TICK | {0}% | {1}B/{2}B", progress, bytes, totalbytes));

                if (diag != null)
                {
                    
                    diag.Value = Convert.ToUInt32(progress);
                    diag.Line3 = bytes + "/" + totalbytes;
                    if (diag.HasUserCancelled)
                    {
                        cancel = true;
                        wc.CancelAsync();
                        //Thread.Sleep(1500);
                        Invoke(new ECHandler(Close));
                        diag.CloseDialog();
                    }
                }
                waitz++;
                Thread.Sleep(100);
            }
            if (diag != null)
                diag.CloseDialog();

            UploadFileCompletedEventArgs e = args;
            UploadValuesCompletedEventArgs c = args1;

            if (exception != null)
            {
                if (!cancel)
                {

                    Exception z = exception;
                    if (TaskDialog.IsPlatformSupported)
                    {
                        TaskDialog dialog = new TaskDialog();
                        dialog.InstructionText = z.Message;
                        dialog.DetailsExpandedText = z.ToString();
                        dialog.Text = "There was an error while uploading your picture :(\r\nPlease click 'More Details' for more information.\r\nIf you keep getting this error please contact the@jariz.pro";
                        dialog.Caption = "Upload failed :(";
                        dialog.Icon = TaskDialogStandardIcon.Error;
                        dialog.Show();
                    }
                    else
                    {
                        MessageBox.Show(z.ToString(), z.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    Invoke(new ECHandler(Close));
                }
            }
            else
            {
                if (e != null) DATAZ = e.Result;
                else DATAZ = c.Result;
                
                
                Invoke(new ECHandler(Close));
            }
        }
        bool notrunnedyet = true;
        public byte[] DATAZ;

        string LE_DERPDERP_FILE = "";

        private static Graphics gfxScreenshot;
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (down)
            {
                if (notrunnedyet)
                {
                    string temp = "";
                    try
                    {
                        notrunnedyet = false;
                        Opacity = 0.0;
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(this.Width, this.Height);
                        gfxScreenshot = Graphics.FromImage(bmp);
                        gfxScreenshot.CopyFromScreen((MousePosition.X - this.Size.Width), (MousePosition.Y - this.Size.Height), 0, 0, this.Size, CopyPixelOperation.SourceCopy);
                        
                        temp = Core.UserDir + "\\" + Path.GetRandomFileName();
                        bmp.Save(temp, System.Drawing.Imaging.ImageFormat.Png);
                        LE_DERPDERP_FILE = temp;

                        if (!imgur)
                        {
                            wc = new WebClient();
                            wc.UploadProgressChanged += new UploadProgressChangedEventHandler(wc_UploadProgressChanged);
                            wc.UploadValuesCompleted += new UploadValuesCompletedEventHandler(wc_UploadValuesCompleted);
                            ThreadPool.QueueUserWorkItem(new WaitCallback(checkProgress));
                            NameValueCollection nvc = new NameValueCollection();
                            nvc.Add("type", "image");
                            nvc.Add("content", Convert.ToBase64String(File.ReadAllBytes(temp)));
                            string key = Core.Settings.IniReadValue("MISC", "userkey");
                            string ukey = "";
                            if (key != string.Empty)
                                ukey = "?userkey=" + Crypto.DecryptStringAES(key, Core.Secret);
                            
                            wc.UploadValuesAsync(new Uri("http://upload.easycaptu.re/"+Application.ProductVersion+ukey), nvc);
                        }
                        else
                        {
                            wc = new WebClient();
                            wc.UploadProgressChanged += new UploadProgressChangedEventHandler(wc_UploadProgressChanged);
                            wc.UploadValuesCompleted += new UploadValuesCompletedEventHandler(wc_UploadValuesCompleted);
                            NameValueCollection values = new NameValueCollection();
                            values.Add("key", Core.ApiKeys.Imgur.DevKey);
                            values.Add("image", Convert.ToBase64String(File.ReadAllBytes(temp)));
                            values.Add("caption", "Uploaded with http://easycaptu.re/ - Capture media from your computer with just a keypress and share it instantly!");
                            string url = "http://api.imgur.com/2/upload";
                            ThreadPool.QueueUserWorkItem(new WaitCallback(checkProgress));
                            string key = Core.Settings.IniReadValue("ScreenCapture", "user_key");
                            if (key != string.Empty)
                            {
                                wc.Headers.Add("Cookie", Crypto.DecryptStringAES(key, Core.Secret));
                                

                                /*OAuth.Manager oauth = new OAuth.Manager();
                                string[] s = key.Split(new string[] { "|" }, StringSplitOptions.None);
                                OAuthBase obase = new OAuthBase();
                                string nurl;
                                string nreq;
                                string sig = obase.GenerateSignature(new Uri("http://api.imgur.com/2/account/images"), Core.ApiKeys.Imgur.Key, Core.ApiKeys.Imgur.Secret, s[0], s[1], "GET", obase.GenerateTimeStamp(), obase.GenerateNonce(), OAuthBase.SignatureTypes.HMACSHA1, out nurl, out nreq);

                                url = nurl + "/?" + nreq + "&oauth_signature=" + sig;
                                
                                /*oauth["consumer_key"] = Core.ApiKeys.Imgur.Key;
                                oauth["consumer_secret"] = Core.ApiKeys.Imgur.Secret;
                                oauth["token"] = s[0];
                                oauth["token_secret"] = s[1];
                                oauth["callback"] = "http://easycaptu.re/";
                                url = "http://api.imgur.com/2/account/images";
                                oauth.GenerateAuthzHeader(url, "POST");
                                foreach (KeyValuePair<string,string> param in oauth._params)
                                {
                                    Out.WriteDebug(param.Key + ": "+param.Value);
                                    //if(param.Key == "token" || param.Key == "callback")
                                        //values.Add("oauth_"+param.Key, param.Value);
                                }*/

                                //Out.WriteDebug("AuthzHeader: " + wc.Headers["Authorization"]);
                            }
                            //if (Core.Imgur_FakeStatus) url += "?_fake_status=200";
                            
                            wc.UploadValuesAsync(new Uri(url), "POST", values);
                        }
                        
                    }
                    catch (Exception z)
                    {
                        Out.WriteError("Something went wrong: " + z.ToString());
                    }
                }
                else Out.WriteDebug("[warning] Screencapture called more then once!!11");
            }
        }

        void wc_UploadValuesCompleted(object sender, UploadValuesCompletedEventArgs e)
        {
            Out.WriteDebug("Uploading == done");
            exception = e.Error;
            
            args1 = e;
            done = true;

            File.Delete(LE_DERPDERP_FILE);
        }

        UploadFileCompletedEventArgs args;
        UploadValuesCompletedEventArgs args1;
        Exception exception = null;
        
        [STAThread]
        void wc_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            exception = e.Error;
            args = e;
            done = true;
            
            File.Delete(LE_DERPDERP_FILE);
        }

        delegate void cliphandler(string clip);

        void wc_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            bytes = Convert.ToInt32(e.BytesSent);
            totalbytes = Convert.ToInt32(e.TotalBytesToSend);
            progress = e.ProgressPercentage * 2;
            
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                abort = true;
                this.Close();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            Opacity = 1;
            Form1 form = new Form1(true);
            form.ShowDialog();
            DATAZ = form.DATAZ;
            abort = form.abort;
            Close();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            WINAPI.SetForegroundWindow(this.Handle);
        }

        
    }
    public class EasyCaptureImageResponse
    {
        public string hash { get; set; }
        public string authcode { get; set; }
        public string img { get; set; }
    }
}
