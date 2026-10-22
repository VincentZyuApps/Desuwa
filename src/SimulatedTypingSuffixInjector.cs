using System.Runtime.InteropServices;

namespace Desuwa;

internal sealed class SimulatedTypingSuffixInjector : ISuffixInjector
{
    public void Inject(string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
        {
            return;
        }

        var inputs = new NativeMethods.INPUT[suffix.Length * 2];
        for (var i = 0; i < suffix.Length; i++)
        {
            var c = suffix[i];
            inputs[i * 2] = new NativeMethods.INPUT
            {
                type = NativeMethods.InputKeyboard,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = NativeMethods.KeyeventfUnicode,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            inputs[i * 2 + 1] = new NativeMethods.INPUT
            {
                type = NativeMethods.InputKeyboard,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = NativeMethods.KeyeventfUnicode | NativeMethods.KeyeventfKeyup,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
