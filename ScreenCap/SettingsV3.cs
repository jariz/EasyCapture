using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EasyCapture
{
    public partial class SettingsV3 : Form
    {

        MMDeviceCollection devices = null;
        MMDevice currdev = null;

        public static Keys getModifier(Keys key)
        {
            Keys modifiers = 0;
            if ((key & Keys.Alt) == Keys.Alt)
                modifiers = modifiers | Keys.Alt;

            if ((key & Keys.Control) == Keys.Control)
                modifiers = modifiers | Keys.Control;

            if ((key & Keys.Shift) == Keys.Shift)
                modifiers = modifiers | Keys.Shift;

            return modifiers;
        }

        public static Keys removeModifiers(Keys key)
        {
            return key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt;
        }

        public SettingsV3()
        {
            InitializeComponent();

            try
            {
                try
                {
                    comboBox1.Text = comboBox1.Items[Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "text"))].ToString();
                    comboBox2.Text = comboBox2.Items[Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "screen"))].ToString();
                    comboBox3.Text = comboBox3.Items[Convert.ToInt32(Core.Settings.IniReadValue("ACTIONS", "sound"))].ToString();
                }
                catch (Exception z)
                {
                    Out.WriteError("Could not recieve index id from settings file " + z.ToString());
                }

                devices = SoundCapture.GetDevices();

                Keys textkeys = (Keys)Convert.ToInt32(Core.Settings.IniReadValue("HotKeys", "text"));
                hotkeyControl1.HotkeyModifiers = (Keys)getModifier(textkeys);
                hotkeyControl1.Hotkey = removeModifiers(textkeys);

                Keys screenkeys = (Keys)Convert.ToInt32(Core.Settings.IniReadValue("HotKeys", "screen"));
                hotkeyControl2.HotkeyModifiers  =(Keys)getModifier(screenkeys);
                hotkeyControl2.Hotkey = removeModifiers(screenkeys);

                Keys soundkeys = (Keys)Convert.ToInt32(Core.Settings.IniReadValue("HotKeys", "sound"));
                hotkeyControl3.HotkeyModifiers = (Keys)getModifier(soundkeys);
                hotkeyControl3.Hotkey = removeModifiers(soundkeys);

                NextTip(null, null);

                SoundCapture.Init();
                currdev = SoundCapture.RecordDevice;

                Out.WriteDebug("--- SELECTED DEVICE ---");
                Out.WriteDebug(SoundCapture.RecordDevice.FriendlyName + " " + SoundCapture.RecordDevice.ID);
                Out.WriteDebug("-- AVAILABLE DEVICES --");
                foreach (MMDevice device in devices)
                {
                    Out.WriteDebug(device.FriendlyName + " " + device.ID);
                    soundDevices.Items.Add(device.DeviceFriendlyName + " ("+device.FriendlyName+")");
                }
                soundDevices.Text = SoundCapture.DeviceName;

                //WasapiLoopbackCapture wasapi = new WasapiLoopbackCapture(SoundCapture.RecordDevice);
                

                checkBox2.Checked = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("ScreenCapture", "freeze")));
                comboBox4.Text = comboBox4.Items[Convert.ToInt32(Core.Settings.IniReadValue("TextCapture", "exposure"))].ToString();

                checkBox5.Checked = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("MISC", "ShowSplash")));
                checkBox7.Checked = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("Update", "Disable")));

                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                string val = ((string)key.GetValue("EasyCapture"));
                checkBox6.Checked = val != string.Empty && val != null;

                CheckAuth();
                CheckAuth2();
                CheckAuth3();

                stuff("general_settings", false);


                bool imgur = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("ScreenCapture", "imgur")));
                bool pastebin = Convert.ToBoolean(Convert.ToInt32(Core.Settings.IniReadValue("TextCapture", "pastebin")));

                imageImgur.Checked = imgur;
                textPB.Checked = pastebin;
                imageEZ.Checked = !imgur;
                textEZ.Checked = !pastebin;

                ignore = false;
            }
            catch (Exception z)
            {
                if (TaskDialog.IsPlatformSupported)
                    TaskDialog.Show(z.ToString(), z.Message, "Loading settings failed");
                else MessageBox.Show(z.ToString(), "Loading settings failed");
                Close();
            }
        }

        void NextTip(object sender, EventArgs bleh)
        {
            //string old = label13.Text.ToString();
            
            //if(label13.Text == old) NextTip(sender, bleh);*/
        }

        bool ignore = true;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                int num = listView1.SelectedIndices[0];
                if (num == 0) stuff("general_settings", false);
                else if (num == 1) stuff("hotkeys_settings", false);
                else if (num == 2) stuff("action_settings", false);
                else if (num == 3) stuff("account_settings", false);
                else if (num == 4) stuff("thirdparty_settings", false);
            }
        }

        void stuff(string name, bool hide)
        {
            Panel[] boxes = new Panel[] { general_settings, hotkeys_settings, action_settings, account_settings, thirdparty_settings };
            foreach (Panel box in boxes)
            {
                if (name == box.Name)
                    box.Visible = !hide;
                else
                    box.Visible = hide;
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void changeAction(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1 && comboBox2.SelectedIndex != -1 && comboBox3.SelectedIndex != -1)
            {
                Core.Settings.IniWriteValue("ACTIONS", "text", Convert.ToString(comboBox1.SelectedIndex));
                Core.Settings.IniWriteValue("ACTIONS", "screen", Convert.ToString(comboBox2.SelectedIndex));
                Core.Settings.IniWriteValue("ACTIONS", "sound", Convert.ToString(comboBox3.SelectedIndex));
            }
        }

        delegate void UpdateHandler(bool positive, string sver);
        void drawUpdate(bool positive, string sver)
        {
            if (!positive)
            {
                updateLabel.Text = "Your EasyCapture is up to date. (" + sver + ")";
                updateIcon.Image = Properties.Resources.accept;
                updatePanel.BackColor = Color.LightGreen;
            }
            else
            {
                updateLabel.Text = "Your EasyCapture is outdated. Please update as soon as possible.";
                updateIcon.Image = Properties.Resources.cross;
                updatePanel.BackColor = Color.LightCoral;
                updateButton.Show();
            }
        }

        void updateFail(object a)
        {
            updateLabel.Text = "Unable to check for updates";
            updateIcon.Image = Properties.Resources.cross;
            updatePanel.BackColor = Color.LightCoral;
        }

        void updateCheck()
        {
            try
            {
                JObject o = (JObject)JsonConvert.DeserializeObject(new WebClient().DownloadString("http://update.easycaptu.re/version"));
                dynamic json = new JsonObject(o);
                this.Invoke(new UpdateHandler(drawUpdate), Core.isNewer(Application.ProductVersion, json.version), json.version);
            }
            catch {
                try
                {
                    this.Invoke(new WaitCallback(updateFail));
                }
                catch { }
            }
        }

        private void ExplorerBrowserTestForm_Load(object sender, EventArgs e)
        {
            new Thread(updateCheck).Start();
        }

        private void soundDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach(MMDevice dev in devices)
            {
                if ((string)soundDevices.Items[soundDevices.SelectedIndex] == dev.DeviceFriendlyName + " (" + dev.FriendlyName + ")")
                {
                    if (Core.Settings.IniReadValue("DEVICE", "device_id") != dev.ID)
                    {
                        Core.Settings.IniWriteValue("DEVICE", "device_id", dev.ID);
                        SoundCapture.Init(dev.ID); //restart soundcapture to load the new device
                        currdev = SoundCapture.RecordDevice;
                    }
                    break;
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //write new settings
            Core.Settings.IniWriteValue("HotKeys", "text", Convert.ToString((int)hotkeyControl1.Hotkey + (int)hotkeyControl1.HotkeyModifiers));
            Core.Settings.IniWriteValue("HotKeys", "screen", Convert.ToString((int)hotkeyControl2.Hotkey + (int)hotkeyControl2.HotkeyModifiers));
            Core.Settings.IniWriteValue("HotKeys", "sound", Convert.ToString((int)hotkeyControl3.Hotkey + (int)hotkeyControl3.HotkeyModifiers));

            //restart hotkeys
            HotKeys.Shutdown();
            HotKeys.Init();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void hotkeyControl1_Enter(object sender, EventArgs e)
        {
            HotKeys.IgnoreTriggers = true;
        }

        private void hotkeyControl1_Leave(object sender, EventArgs e)
        {
            HotKeys.IgnoreTriggers = false;
        }

        void TellYourFriends()
        {
            TaskDialog taskd = new TaskDialog();
            taskd.InstructionText = "Please tell your friends about EasyCapture";
            taskd.Text = "When you're using a alternative filehost, people won't be able to see that you're using EasyCapture :(\r\nPlease tell your friends about EasyCapture, we'd really appreciate it. It's all we ask.";
            taskd.Icon = TaskDialogStandardIcon.Information;
            taskd.Caption = taskd.InstructionText;
            taskd.Show();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("ScreenCapture", "freeze", Convert.ToString(Convert.ToInt32(checkBox2.Checked)));
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("TextCapture", "exposure", Convert.ToString(comboBox4.SelectedIndex));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var st = Credentials.AuthPastebin(0);
            if(st != string.Empty)
                Core.Settings.IniWriteValue("TextCapture", "user_key", st);
            CheckAuth();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("TextCapture", "user_key", string.Empty);
            TaskDialog.Show("Pastebin authorization information removed", "Authorization removed", "Pastebin");
            CheckAuth();
        }

        void CheckAuth()
        {
            if (Core.Settings.IniReadValue("TextCapture", "user_key") != string.Empty)
            {
                label21.Text = "Account added";
                button3.Enabled = true;
            }
            else
            {
                button3.Enabled = false;
                label21.Text = "No account currently added";
            }
        }

        void CheckAuth2()
        {
            if (Core.Settings.IniReadValue("ScreenCapture", "user_key") != string.Empty)
            {
                label25.Text = "Account added";
                button4.Enabled = true;
            }
            else
            {
                button4.Enabled = false;
                label25.Text = "No account currently added";
            }
        }

        void CheckAuth3()
        {
            if (Core.Settings.IniReadValue("MISC", "userkey") != string.Empty && Core.Settings.IniReadValue("MISC", "username") != string.Empty)
            {
                loggedin.Show();
                loggedout.Hide();
                username.Text = Core.Settings.IniReadValue("MISC", "username");
            }
            else
            {
                loggedout.Show();
                loggedin.Hide();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://easycaptu.re/register");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string str = Credentials.AuthImgur(0);
            if (str != string.Empty)
                Core.Settings.IniWriteValue("ScreenCapture", "user_key", str);
            CheckAuth2();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("ScreenCapture", "user_key", string.Empty);
            TaskDialog.Show("Imgur authorization information removed", "Authorization removed", "Imgur");
            CheckAuth2();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("MISC", "ShowSplash", Convert.ToString(Convert.ToInt32(checkBox5.Checked)));
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (checkBox6.Checked)
                key.SetValue("EasyCapture", "\"" + Application.ExecutablePath + "\" /s");
            else key.DeleteValue("EasyCapture");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            new SettingsAdvanced().ShowDialog();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            Core.Settings.IniWriteValue("Update", "Disable", Convert.ToString(Convert.ToInt32(checkBox7.Checked)));
        }

        private void volumeMeterRefresher_Tick(object sender, EventArgs e)
        {
            if (currdev.AudioMeterInformation.PeakValues.Count > 1)
            {
                if (currdev.AudioMeterInformation.PeakValues[0] != 0 && currdev.AudioMeterInformation.PeakValues[1] != 0)
                {
                    label29.Visible = false;
                    //volumeMeter1.Amplitude = left;
                    //volumeMeter2.Amplitude = right;

                    progressBar1.Value = Convert.ToInt32(Math.Round(currdev.AudioMeterInformation.PeakValues[0] * 100));
                    progressBar2.Value = Convert.ToInt32(Math.Round(currdev.AudioMeterInformation.PeakValues[1] * 100));
                }
                else label29.Visible = true;
            }
            else label29.Visible = true;
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            Close();
            System.Diagnostics.Process.Start(Application.ExecutablePath, "/i");
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        delegate void TTHand(IWin32Window win);
        private void SettingsV3_Shown(object sender, EventArgs e)
        {
            if (new Random().Next(0, 20) == 10)
            {
                toolTip1.Show("As much as we like to keep it free, servers cost money.\r\nIf you love this tool and want to support us, please considering donating.", button9, -360, -80);
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o) { Thread.Sleep(4000); Invoke(new TTHand(toolTip1.Hide), (button9)); }));
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Core.openLink("http://easycaptu.re/donate");
        }

        private void textPB_CheckedChanged(object sender, EventArgs e)
        {
            if (textPB.Checked && !ignore) TellYourFriends();
            Core.Settings.IniWriteValue("TextCapture", "pastebin", Convert.ToString(Convert.ToInt32(textPB.Checked)));

            label19.Enabled = textPB.Checked;
            comboBox4.Enabled = textPB.Checked;

            CheckAuth();
        }

        private void imageImgur_CheckedChanged(object sender, EventArgs e)
        {
            if (imageImgur.Checked && !ignore) TellYourFriends();
            Core.Settings.IniWriteValue("ScreenCapture", "imgur", Convert.ToString(Convert.ToInt32(imageImgur.Checked)));

            CheckAuth2();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Core.openLink("http://easycaptu.re/whyaccount");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string[] res = Credentials.AuthEasyCapture(0);
            Core.Settings.IniWriteValue("MISC", "userkey", res[0]);
            Core.Settings.IniWriteValue("MISC", "username", res[1]);

            CheckAuth3();
        }
    }
}
