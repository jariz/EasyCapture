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
    public partial class Reminder : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams ret = base.CreateParams;
                //ret.Style = /*(int)WINAPI.WindowStyles.WS_THICKFRAME | */(int)WINAPI.WindowStyles.WS_CHILD;
                ret.ExStyle |= (int)WINAPI.WindowStylesEx.WS_EX_NOACTIVATE | (int)WINAPI.WindowStylesEx.WS_EX_TOPMOST;
                ret.X = this.Location.X;
                ret.Y = this.Location.Y;
                return ret;
            }
        }

        public Reminder(string instruction, string text, int time)
        {
            InitializeComponent();

            Opacity = 0.0f;
            timer2.Interval = time;
            label1.Text = instruction;
            label2.Text = text;
        }

        private void Reminder_Load(object sender, EventArgs e)
        {
            Rectangle workarea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(workarea.Width - this.Width, workarea.Height - this.Height);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!reverse)
            {
                Opacity += 0.10;
                if (this.Opacity >= 1.0)
                {
                    timer1.Stop();
                    timer2.Start();
                }
            }
            else
            {
                Opacity -= 0.10;
                if (this.Opacity == 0.0)
                {
                    timer1.Stop();
                    Close();
                }
            }
        }

        bool reverse = false;

        private void timer2_Tick(object sender, EventArgs e)
        {
            reverse = true;
            timer2.Stop();
            timer1.Start();
        }
    }
}
