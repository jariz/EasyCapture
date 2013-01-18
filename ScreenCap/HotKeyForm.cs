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
    public partial class HotKeyForm : Form
    {
        public HotKeyForm(int id, string captype)
        {
            InitializeComponent();

            Visible = false;
            this.id = id;
            _captype = captype;
        }

        public string CapType
        {
            get
            {
                return _captype;
            }
        }
        
        string _captype;

        int id = 0;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == HotKeys.WM_HOTKEY)
                HotKeys.TriggerKey(this.id);
        }

        

        private void HotKeyForm_Load(object sender, EventArgs e)
        {
            WINAPI.ShowWindow(this.Handle, WINAPI.ShowWindowCommands.Hide);
        }

        private void HotKeyForm_Shown(object sender, EventArgs e)
        {
            //Hide();
        } 
    }
}
