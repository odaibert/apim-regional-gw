
namespace Common.Enums
{
    /// <summary>
    /// Enumeration defining the Emulator Device Types.
    /// </summary>
    /// <remarks>
    /// This enum should refelect the type of the device, the emulator is currently running on, e.g. desktop, tablet, IoT, etc.
    /// </remarks>
    public enum EmulatorDeviceTypeEnum
    {
        // Unknown emulator device type
        Unknown = 0,
        Desktop = 1,
        Tablet = 2,
        Mobile = 3,
        IoT = 4,
        SurfaceHub = 5,
        XBox = 6,
    }
}
