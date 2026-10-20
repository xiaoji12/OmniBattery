using System;
using EasyBluetooth.Models;

namespace EasyBluetooth.Services.Providers
{
    public interface IBatteryProvider
    {
        string Name { get; }
        bool CanHandle(string devicePath, string productName, string brand);
        bool TryQueryBattery(string devicePath, out int batteryLevel, out bool isCharging);
    }
}
