using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using EasyBluetooth.Models;

namespace EasyBluetooth.Services
{
    public sealed class RtssExporter : IDisposable
    {
        private const string MapName = "AIDA64_SensorValues";
        private const int MapCapacity = 256 * 1024;

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;

        public void Export(IList<DeviceInfo> devices)
        {
            try
            {
                EnsureOpened();
                if (_accessor == null) return;

                var sb = new StringBuilder();
                int count = Math.Min(devices.Count, 10);
                for (int i = 0; i < count; i++)
                {
                    var dev = devices[i];
                    string prefix = string.Format("EB_D{0}", i + 1);
                    AppendSensor(sb, prefix + "_NAME", string.Format("Device{0} Name", i + 1), dev.DisplayName);
                    AppendSensor(sb, prefix + "_BATTERY", string.Format("Device{0} Battery", i + 1), dev.Battery.HasValue ? dev.Battery.Value.ToString() : "--");
                    AppendSensor(sb, prefix + "_CHARGING", string.Format("Device{0} Charging", i + 1), dev.IsCharging ? "1" : "0");
                    AppendSensor(sb, prefix + "_TYPE", string.Format("Device{0} Type", i + 1), dev.Category.ToString());
                }

                byte[] bytes = Encoding.Default.GetBytes(sb.ToString());
                byte[] buffer = new byte[MapCapacity];
                int copyLen = Math.Min(bytes.Length, MapCapacity - 1);
                Array.Copy(bytes, buffer, copyLen);

                _accessor.WriteArray(0, buffer, 0, buffer.Length);
                _accessor.Flush();
            }
            catch { }
        }

        private static void AppendSensor(StringBuilder sb, string id, string label, string val)
        {
            sb.AppendFormat("<{0} id=\"{0}\" label=\"{1}\">{2}</{0}>\n", id, label, val);
        }

        private void EnsureOpened()
        {
            if (_mmf != null && _accessor != null) return;
            try
            {
                _mmf = MemoryMappedFile.CreateOrOpen(MapName, MapCapacity, MemoryMappedFileAccess.ReadWrite);
                _accessor = _mmf.CreateViewAccessor(0, MapCapacity, MemoryMappedFileAccess.Write);
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                if (_accessor != null) { _accessor.Dispose(); _accessor = null; }
                if (_mmf != null) { _mmf.Dispose(); _mmf = null; }
            }
            catch { }
        }
    }
}
