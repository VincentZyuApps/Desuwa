using System.Runtime.InteropServices;

namespace Desuwa;

internal sealed class ClipboardSuffixInjector : ISuffixInjector
{
    public void Inject(string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
        {
            return;
        }

        string? originalText = null;
        var hadClipboardText = false;

        try
        {
            try
            {
                if (NativeMethods.IsClipboardFormatAvailable(NativeMethods.CfUnicodetext) &&
                    NativeMethods.OpenClipboard(IntPtr.Zero))
                {
                    var hData = NativeMethods.GetClipboardData(NativeMethods.CfUnicodetext);
                    if (hData != IntPtr.Zero)
                    {
                        var ptr = NativeMethods.GlobalLock(hData);
                        if (ptr != IntPtr.Zero)
                        {
                            try
                            {
                                originalText = Marshal.PtrToStringUni(ptr);
                                hadClipboardText = originalText != null;
                            }
                            finally
                            {
                                NativeMethods.GlobalUnlock(hData);
                            }
                        }
                    }
                    NativeMethods.CloseClipboard();
                }
            }
            catch
            {
                // 忽略读取原剪贴板异常
            }

            SetClipboardText(suffix);

            // 发送 Ctrl+V
            NativeMethods.KeybdEvent(NativeMethods.VkControl, 0, 0, 0);
            NativeMethods.KeybdEvent(NativeMethods.VkV, 0, 0, 0);
            NativeMethods.KeybdEvent(NativeMethods.VkV, 0, NativeMethods.KeyeventfKeyup, 0);
            NativeMethods.KeybdEvent(NativeMethods.VkControl, 0, NativeMethods.KeyeventfKeyup, 0);

            Thread.Sleep(25);
        }
        finally
        {
            try
            {
                if (hadClipboardText && originalText != null)
                {
                    SetClipboardText(originalText);
                }
                else
                {
                    if (NativeMethods.OpenClipboard(IntPtr.Zero))
                    {
                        NativeMethods.EmptyClipboard();
                        NativeMethods.CloseClipboard();
                    }
                }
            }
            catch
            {
                // 忽略恢复原剪贴板异常
            }
        }
    }

    private static void SetClipboardText(string text)
    {
        for (var i = 0; i < 5; i++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                NativeMethods.EmptyClipboard();
                var bytes = (nuint)((text.Length + 1) * 2);
                var hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GmemMoveable, bytes);
                if (hGlobal != IntPtr.Zero)
                {
                    var target = NativeMethods.GlobalLock(hGlobal);
                    if (target != IntPtr.Zero)
                    {
                        Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                        Marshal.WriteInt16(target, text.Length * 2, 0);
                        NativeMethods.GlobalUnlock(hGlobal);
                        NativeMethods.SetClipboardData(NativeMethods.CfUnicodetext, hGlobal);
                    }
                }
                NativeMethods.CloseClipboard();
                return;
            }
            Thread.Sleep(10);
        }
    }
}
