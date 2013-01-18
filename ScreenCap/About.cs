using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EasyCapture
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        bool or = false;

        private void About_Load(object sender, EventArgs e)
        {
            try
            {
                or = true;
                webBrowser1.DocumentText = Properties.Resources.credits.Replace("{0}", Application.ProductVersion);
                or = false;
            }
            catch (Exception z)
            {
                webBrowser1.DocumentText = "Failed loading credits... :\\<br><span style='color:red'>" + z.ToString().Replace("\n", "<br>") + "</span>";
            }
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (!or)
            {
                e.Cancel = true;
                Core.openLink(e.Url.OriginalString);
            }
        }
    }
}
