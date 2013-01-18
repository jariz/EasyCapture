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
    public partial class SettingsAdvanced : Form
    {
        public SettingsAdvanced()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(!init)
                if (WINAPI.GetConsoleWindow() == IntPtr.Zero)
                {
                    Core.startConsole();
                    Console.Title = "EasyCapture.Console";
                    Console.Clear();
                    Out.Write(Out.Buffer, true, new object[] { });
                    Core.Settings.IniWriteValue("MISC", "Console", "1");
                }
                else
                {
                    Core.Settings.IniWriteValue("MISC", "Console", "0");
                    WINAPI.FreeConsole();
                }
        }
        bool init = false;

        private void SettingsAdvanced_Load(object sender, EventArgs e)
        {
            init = true;
            checkBox1.Checked = WINAPI.GetConsoleWindow() != IntPtr.Zero;
            init = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("MISC", "alpha", Convert.ToInt32(checkBox2.Checked).ToString());
        }
    }
}
