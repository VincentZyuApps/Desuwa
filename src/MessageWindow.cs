using System.Runtime.InteropServices;

namespace Desuwa;

internal sealed class MessageWindow : IDisposable
{
    private static readonly string ClassName = $"DesuwaMessageWindow_{Guid.NewGuid():N}";
    private readonly NativeMethods.WndProcDelegate _wndProc;
    private bool _disposed;

    public IntPtr Handle { get; }

    public event Action<int>? HotkeyPressed;

    public MessageWindow()
    {
        _wndProc = CustomWndProc;
        var wndClass = new NativeMethods.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = NativeMethods.GetModuleHandle(null),
            lpszClassName = ClassName
        };

        NativeMethods.RegisterClassEx(ref wndClass);

        Handle = NativeMethods.CreateWindowEx(
            0,
            ClassName,
            "DesuwaHotkeySink",
            0,
            0, 0, 0, 0,
            NativeMethods.HwndMessage,
            IntPtr.Zero,
            wndClass.hInstance,
            IntPtr.Zero);
    }

    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WmHotkey)
        {
            HotkeyPressed?.Invoke(wParam.ToInt32());
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Handle != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(Handle);
        }
    }
}
