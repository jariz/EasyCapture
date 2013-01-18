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
    public partial class SoundScreen : Form
    {
        public SoundScreen()
        {
            InitializeComponent();

            Core.STOPSS += new Core.CaptureHandler(Core_STOPSS);
        }

        void Core_STOPSS()
        {
            if (InvokeRequired)
            {
                Invoke(new Core.CaptureHandler(Core_STOPSS));
                return;
            }
            _override = true;
            Close();
        }

        bool reverse = false;
        bool _override = false;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Opacity <= 1.0)
            {
                if (!reverse)
                {
                    Opacity += 0.10;
                    if (this.Opacity >= 1.0)
                    {
                        timer1.Stop();
                    }
                }
                else
                {
                    Opacity -= 0.10;
                    if (this.Opacity == 0.0)
                    {
                        timer1.Stop();
                        _override = false;
                        Close();
                    }
                }
            }
        }

        private void SoundScreen_Load(object sender, EventArgs e)
        {
            Rectangle workarea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(workarea.Width - this.Width, workarea.Height - this.Height);
        }

        private void SoundScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_override)
                e.Cancel = true;
            else
            {
                e.Cancel = true;
                reverse = true;
                _override = true;
                timer1.Start();
            }
        }
    }
}
