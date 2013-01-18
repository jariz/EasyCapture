using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EasyCapture
{
    public partial class ImgurAuth : Form
    {
        public ImgurAuth(Uri auth)
        {
            InitializeComponent();

            webBrowser1.Url = auth;
        }

        private void ImgurAuth_Load(object sender, EventArgs e)
        {

        }

        private void ImgurAuth_Shown(object sender, EventArgs e)
        {
            //WINAPI.SetForegroundWindow(this.Handle);
        }

        public string pin = "";

        bool st = true;
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (!st)
            {
                try
                {
                    string re1 = ".*?";	// Non-greedy match on filler
                    string re2 = "(accent green\">)";	// Tag 1
                    string re3 = "(.*?)";	// Non-greedy match on filler
                    string re4 = "(<\\/span>)";	// Tag 2

                    Regex r = new Regex(re1 + re2 + re3 + re4, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match m = r.Match(webBrowser1.DocumentText);
                    if (m.Success)
                    {
                        pin = m.Groups[2].ToString();
                    }
                }
                catch
                {
                    pin = "";
                }
                Close();
            }
            else st = false;
        }
    }
}
