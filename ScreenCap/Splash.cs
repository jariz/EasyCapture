using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;

namespace EasyCapture
{
    public partial class Splash : GlassForm
    {
        public Splash()
        {
            InitializeComponent();
            defsize = Size;
        }

        bool logo = false;

        void reDraw()
        {
            InvokePaint(this, new PaintEventArgs(this.CreateGraphics(), this.DisplayRectangle));
        }

        private void Splash_Load(object sender, EventArgs e)
        {
            reDraw();
        }

        void Core_statusChanged(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Core.statusHandler(Core_statusChanged), status);
                return;
            }
            if (status != "Waiting for input")
                reDraw();
            else Close();
        }

        private void Splash_Shown(object sender, EventArgs e)
        {
            WINAPI.SetForegroundWindow(this.Handle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (logo)
                WINAPI.DrawTextOnGlass(Handle, "EasyCapture", new Font("Segoe UI", 66f), DisplayRectangle, 5);
            else
                WINAPI.DrawTextOnGlass(Handle, Core.Status, new Font("Segoe UI", 24f), DisplayRectangle, 5);
        }
        Size defsize;
        private void Splash_Resize(object sender, EventArgs e)
        {
            Size = defsize;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            logo = false;
            Core.statusChanged += new Core.statusHandler(Core_statusChanged);
        }

        private void Splash_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Core.Status != "Waiting for input")
                Environment.Exit(1);
        }

        private void Splash_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}
