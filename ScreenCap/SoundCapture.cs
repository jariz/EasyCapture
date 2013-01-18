using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Wave.Compression;
using NAudio.FileFormats.Mp3;

namespace EasyCapture
{
    class SoundCapture
    {
        public static MMDevice RecordDevice
        {
            get
            {
                Init(_id);
                return _recorddevice;
            }
        }

        public static string ID
        {
            get
            {
                if (_id == string.Empty) _id = _recorddevice.ID;
                return _id;
            }
        }

        static string _id = "";

        public static string DeviceName
        {
            get
            {
                Init();
                return _recorddevice.DeviceFriendlyName + " (" + _recorddevice.FriendlyName + ")";
            }
        }

        static MMDevice _recorddevice = null;

        public static MMDeviceCollection GetDevices()
        {
            return new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
        }

        public static void Init()
        {
            Init(string.Empty);
        }

        public static void Init(string id)
        {
            try
            {
                if (id != string.Empty)
                {
                    _id = Core.Settings.IniReadValue("DEVICE", "device_id");
                    _recorddevice = new MMDeviceEnumerator().GetDevice(Core.Settings.IniReadValue("DEVICE", "device_id"));
                }
                else
                {
                    _recorddevice = new MMDeviceEnumerator().GetDevice(id);  
                }
                GC.Collect();
            }
            catch (Exception z)
            {
                _recorddevice = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
        }

        static WaveFileWriter writer;
        static WasapiCapture cap;
        public static void Record()
        {
            Init();
            _filename = Core.UserDir + "\\" + Path.GetRandomFileName().Replace(".", "") + ".wav";
            
            if (_recorddevice.DataFlow == DataFlow.Render)
            {                
                cap = new WasapiLoopbackCapture(_recorddevice);
                writer = new WaveFileWriter(_filename, cap.WaveFormat);
                cap.RecordingStopped += new EventHandler<StoppedEventArgs>(cap_RecordingStopped);
                cap.StartRecording();
                cap.DataAvailable += new EventHandler<WaveInEventArgs>(cap_DataAvailable);
                _running = true;
            }
            else
            {
                cap = new WasapiCapture(_recorddevice);
                writer = new WaveFileWriter(_filename, cap.WaveFormat);
                cap.RecordingStopped += new EventHandler<StoppedEventArgs>(cap_RecordingStopped);
                cap.StartRecording();
                cap.DataAvailable += new EventHandler<WaveInEventArgs>(cap_DataAvailable);
                _running = true;
           }
        }

        public static string Stop()
        {
            Out.WriteLine("SoundCapture stopped, aborting and flushing...");
            cap.StopRecording();
            cap.Dispose();
            cap = null;
            writer.Close();
            writer = null;
            _running = false;
            return _filename;
        }

        static string _filename;

        static void cap_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _running = false;
        }

        static void cap_DataAvailable(object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public static bool Recording
        {
            get
            {
                return _running;
            }
        }
        static bool _running = false;
    }
}
