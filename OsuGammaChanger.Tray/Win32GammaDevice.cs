using System.Runtime.InteropServices;
using OsuGammaChanger.Core;

namespace OsuGammaChanger.Tray;

internal sealed class Win32GammaDevice : IGammaDevice, IDisposable
{
    private readonly IntPtr _deviceContext;

    public Win32GammaDevice(string deviceName)
    {
        DeviceName = deviceName;
        _deviceContext = CreateDC(null, deviceName, null, IntPtr.Zero);
    }

    public string DeviceName { get; }
    public bool IsValid => _deviceContext != IntPtr.Zero;

    public bool TryGetCurrentRamp(out GammaRamp ramp, out string? error)
    {
        ramp = new GammaRamp(new ushort[256], new ushort[256], new ushort[256]);
        error = null;

        if (!IsValid)
        {
            error = $"Invalid device context for {DeviceName}.";
            return false;
        }

        var native = new NativeRamp
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };
        if (!GetDeviceGammaRamp(_deviceContext, ref native))
        {
            error = $"GetDeviceGammaRamp failed for {DeviceName}.";
            return false;
        }

        ramp = new GammaRamp(native.Red, native.Green, native.Blue);
        return true;
    }

    public bool TrySetRamp(GammaRamp ramp, out string? error)
    {
        error = null;

        if (!IsValid)
        {
            error = $"Invalid device context for {DeviceName}.";
            return false;
        }

        var native = new NativeRamp
        {
            Red = ramp.Red.ToArray(),
            Green = ramp.Green.ToArray(),
            Blue = ramp.Blue.ToArray()
        };

        if (!SetDeviceGammaRamp(_deviceContext, ref native))
        {
            error = $"SetDeviceGammaRamp failed for {DeviceName}.";
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        if (_deviceContext != IntPtr.Zero)
            DeleteDC(_deviceContext);
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateDC(string? driver, string device, string? output, IntPtr initData);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool GetDeviceGammaRamp(IntPtr hdc, ref NativeRamp ramp);

    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr hdc, ref NativeRamp ramp);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRamp
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Blue;
    }
}
