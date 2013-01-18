using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace EasyCapture
{
    public partial class PastebinCaptcha : Form
    {
        public PastebinCaptcha(string link, string cheader)
        {
            InitializeComponent();

            HttpWebRequest wc = (HttpWebRequest)HttpWebRequest.Create("http://pastebin.com" + link);
            wc.Headers.Add("Cookie", cheader);
            wc.UserAgent = "EC RequestCaptcha " + Application.ProductVersion;
            pictureBox1.Image = Bitmap.FromStream(wc.GetResponse().GetResponseStream());
        }

        private void PastebinCaptcha_Load(object sender, EventArgs e)
        {

        }

        public string captcha = "";

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                captcha = textBox1.Text;
                DialogResult = System.Windows.Forms.DialogResult.Yes;
                Close();
            }
        }

        private void PastebinCaptcha_Shown(object sender, EventArgs e)
        {
            WINAPI.SetForegroundWindow(this.Handle);
        }
    }
}
