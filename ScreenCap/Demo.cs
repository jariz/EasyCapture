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
    public partial class Demo : Form
    {
        public Demo()
        {
            InitializeComponent();
        }

        private void Demo_Load(object sender, EventArgs e)
        {
            Core.ScreenCaptureTrigger += new Core.CaptureHandler(Core_ScreenCaptureTrigger);
            Core.ScreenCaptureUp += new Core.CaptureHandler(Core_ScreenCaptureUp);
        }

        void Core_ScreenCaptureUp()
        {
            if (InvokeRequired)
            {
                Invoke(new Core.CaptureHandler(Core_ScreenCaptureUp));
                return;
            }
            screencap_instr.Show();
        }

        void Core_ScreenCaptureTrigger()
        {
            if (InvokeRequired)
            {
                Invoke(new Core.CaptureHandler(Core_ScreenCaptureTrigger));
                return;
            }
            screencap_instr.Hide();
            pressthefollowing.Hide();
            panel1.Hide();
            success.Show();
            next.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Demo_Shown(object sender, EventArgs e)
        {
            WINAPI.SetForegroundWindow(this.Handle);
        }

        private void next_Click(object sender, EventArgs e)
        {
            if (step_1.Visible)
            {
                step_1.Hide();
                step_2.Show();
                back.Enabled = true;
                next.Enabled = false;
                Core.TextCaptureTrigger += new Core.CaptureHandler(Core_TextCaptureTrigger);
                Core.TextCaptureUp += new Core.CaptureHandler(Core_TextCaptureUp);
                pictureBox1.BackgroundImage = Properties.Resources.turtle;
            }
        }

        void Core_TextCaptureUp()
        {
            if (InvokeRequired)
            {
                Invoke(new Core.CaptureHandler(Core_TextCaptureUp));
                return;
            }
            
        }

        void Core_TextCaptureTrigger()
        {
            if (InvokeRequired)
            {
                Invoke(new Core.CaptureHandler(Core_TextCaptureTrigger));
                return;
            }
            label1.Hide();
            textBox1.Hide();
            panel7.Hide();
            success2.Show();

            next.Enabled = true;
        }
    }
}
