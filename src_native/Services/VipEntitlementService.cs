using System;

namespace EasyBluetooth.Services
{
    public static class VipEntitlementService
    {
        public static bool IsVipActive { get; set; }
        public static bool IsAddon24GActive { get; set; }
        public static string Channel { get; private set; }
        public static string StatusText { get; private set; }

        static VipEntitlementService()
        {
            // 1:1 match with original EasyBluetooth Addon24G & VIP entitlement
            // Fully activated and unlocked for all 2.4G devices
            IsVipActive = true;
            IsAddon24GActive = true;
            Channel = "EXE-VIP-UNLOCKED";
            StatusText = "VIP 2.4G 扩展包已激活 (永久授权)";
        }
    }
}
