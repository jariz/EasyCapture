using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel; 

namespace EasyCapture
{
    class HotKeys
    {
        const int KEYEVENTF_KEYUP = 0x2;
        const int KEYEVENTF_KEYDOWN = 0x0;
        const byte VK_CONTROL = 0x11;

        public static bool IgnoreTriggers
        {
            get
            {
                return _ignore;
            }
            set
            {
                _ignore = value;
            }
        }

        static bool _ignore = false;
        
        static void SendCtrlC(IntPtr hWnd)
        {
            WINAPI.SetForegroundWindow(hWnd);
            WINAPI.keybd_event(VK_CONTROL, 0, 0, 0);
            WINAPI.keybd_event(0x43, 0, 0, 0);
            WINAPI.keybd_event(0x43, 0, KEYEVENTF_KEYUP, 0);
            WINAPI.keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        }

        public static void Init()
        {
            AddKey(CapType.screen);
            AddKey(CapType.sound);
            AddKey(CapType.text);
        }

        static string internalCopy()
        {
            try
            {
                if (Clipboard.ContainsData(DataFormats.Text))
                    return Clipboard.GetText();
                else
                    return "";
            }
            catch (Exception z)
            {
                /*IntPtr clipwindow = WINAPI.GetOpenClipboardWindow();
                if (clipwindow != IntPtr.Zero)
                {
                    StringBuilder sb = new StringBuilder();
                    WINAPI.GetWindowText(clipwindow.ToInt32(), sb, 500);
                    Core.FatalError(new Exception("EasyCapture was unable to get clipboard contents because the window '" + sb.ToString() + "' (0x" + clipwindow + ") blocked EasyCapture from getting clipboard contents.",z));
                    return "";
                }
                else
                {
                    Core.FatalError(new Exception("EasyCapture was unable to get clipboard contents", z));
                    return "";
                }*/
                return "";
            }
        }

        public static string Copy()
        {
            try
            {
                //Clipboard.Clear();
                IntPtr curr = WINAPI.GetForegroundWindow();
                //StringBuilder build = new StringBuilder();
                //WINAPI.GetWindowText(curr.ToInt32(), build, 500);
                //Out.WriteDebug("Copying selected text from " + build.ToString());
                SendCtrlC(curr);
                Thread.Sleep(250);
                
                string txt = internalCopy();
                int i = 0;
                while (txt == "")
                {
                    i++;
                    if (WINAPI.GetForegroundWindow() != curr)
                    {
                        Out.WriteLine("Foreground window changed, aborting");
                        break;
                    }
                    if (i == 20)
                    {
                        Out.WriteLine("Waited to long to get text, aborting");
                        break;
                    }
                    SendCtrlC(curr);
                    Thread.Sleep(250);
                    txt = internalCopy();
                }
                Clipboard.Clear();
                Out.WriteLine("Got selected text. Selected text is "+txt.Length + " characters long");
                return txt;
            }
            catch (Exception z) { Out.WriteError("Failed to get selected text: " + z.ToString()); return ""; }
        }

        #region fields
        public static int MOD_ALT = 0x1;
        public static int MOD_CONTROL = 0x2;
        public static int MOD_SHIFT = 0x4;
        public static int MOD_WIN = 0x8;
        public static int WM_HOTKEY = 0x312;
        #endregion

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static void RegisterHotKey(Form f, Keys key, int id)
        {
            int modifiers = 0;

            if ((key & Keys.Alt) == Keys.Alt)
                modifiers = modifiers | MOD_ALT;

            if ((key & Keys.Control) == Keys.Control)
                modifiers = modifiers | MOD_CONTROL;

            if ((key & Keys.Shift) == Keys.Shift)
                modifiers = modifiers | MOD_SHIFT;


            Keys k = key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt;

            Out.WriteLine("Modifier: " + modifiers + " Key(s): " + (int)k);
            if (!RegisterHotKey((IntPtr)f.Handle, id, (int)modifiers, (int)k))
                Out.WriteError("Unable to RegisterKey, error " + new Win32Exception(Marshal.GetLastWin32Error()));
            else Out.WriteDebug("Registering complete.");
        }

        private delegate void Func();

        public static void UnregisterHotKey(Form f, int id)
        {
            try
            {
                UnregisterHotKey(f.Handle, id); // modify this if you want more than one hotkey
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public static void TriggerKey(int id)
        {
            if (!_ignore)
            {
                if (!RegisteredKeys.ContainsKey(id))
                    Core.FatalError(new HotkeyException());
                else
                {
                    switch ((CapType)Enum.Parse(typeof(CapType), RegisteredKeys[id].CapType))
                    {
                        case CapType.text:
                            Core.doText();
                            break;
                        case CapType.screen:
                            Core.doCapture();
                            break;
                        case CapType.sound:
                            Core.doSound();
                            break;
                    }
                }
            }
            else Out.WriteDebug("Ignored TriggerKey(" + id + ")");
        }

        public enum CapType
        {
            screen, sound, text
        }

        public static Dictionary<int, HotKeyForm> RegisteredKeys = new Dictionary<int, HotKeyForm>(); // hotkey reciever, hotkey id string

        public static void AddKey(CapType type)
        {
            Thread.Sleep(100);
            ThreadPool.QueueUserWorkItem(new WaitCallback(AddKeyThread), new object[] { type, new Random().Next() });
        }

        static void AddKeyThread(object typeraw)
        {
            object[] objects = (object[])typeraw;
            CapType type = (CapType)objects[0];
            int random = (int)objects[1];
            
            string hotkey = Core.Settings.IniReadValue("HotKeys", type.ToString());
            if (hotkey == "" || hotkey == "0")
            {
                Out.WriteError("Hotkey " + type.ToString() + " was not found in config file!, Skipping!");
                return;
            }
            Keys key = (Keys)Convert.ToInt32(hotkey);
             // randomfix
            int id = random;
            HotKeyForm form = new HotKeyForm(id, type.ToString());
            Out.WriteDebug("Registering keycode " + (int)key + " with ID " + id);
            RegisterHotKey(form, key, id);
            RegisteredKeys.Add(id, form);
            form.ShowDialog();
        }

        delegate void CloseCloseCloseCloseCLOSECLOOOOOOOSEEEEE();
        delegate void UnregisterHandler(Form f,int id);

        public static void Shutdown()
        {
            foreach (KeyValuePair<int, HotKeyForm> val in new Dictionary<int,HotKeyForm>(RegisteredKeys))
            {
                val.Value.Invoke(new UnregisterHandler(UnregisterHotKey), new object[] { val.Value, val.Key });
                val.Value.Invoke(new CloseCloseCloseCloseCLOSECLOOOOOOOSEEEEE(val.Value.Close));
                RegisteredKeys.Remove(val.Key);
            }
            Out.WriteLine("Unregistered all hotkeys!");
        }

        class HotkeyException : Exception
        {
            
        }
    }
}