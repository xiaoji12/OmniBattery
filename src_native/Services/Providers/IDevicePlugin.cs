using System;

namespace EasyBluetooth.Services.Providers
{
    public interface IDevicePlugin
    {
        string Id { get; }
        string Name { get; }
        string Brand { get; }
        string[] SupportedVids { get; }
        IBatteryProvider Provider { get; }
        bool IsEnabled { get; set; }
    }
}
