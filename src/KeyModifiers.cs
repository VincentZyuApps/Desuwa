namespace Desuwa;

[Flags]
public enum KeyModifiers : uint
{
    None = 0,
    Alt = NativeMethods.ModAlt,
    Control = NativeMethods.ModControl,
    Shift = NativeMethods.ModShift
}
