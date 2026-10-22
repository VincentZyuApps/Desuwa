using System.Text.Json.Serialization;

namespace Desuwa;

public sealed class HotkeyGesture
{
    private const KeyModifiers AllowedModifiers = KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift;

    public bool Enabled { get; set; }

    public VirtualKey Key { get; set; }

    public KeyModifiers Modifiers { get; set; }

    [JsonIgnore]
    public KeyModifiers NormalizedModifiers => NormalizeModifiers(Modifiers);

    public static HotkeyGesture Disabled()
    {
        return new HotkeyGesture
        {
            Enabled = false,
            Key = VirtualKey.None,
            Modifiers = KeyModifiers.None
        };
    }

    public static HotkeyGesture CreateDefault(VirtualKey key, KeyModifiers modifiers = KeyModifiers.Control | KeyModifiers.Alt)
    {
        return new HotkeyGesture
        {
            Enabled = true,
            Key = key,
            Modifiers = NormalizeModifiers(modifiers)
        };
    }

    public HotkeyGesture Clone()
    {
        return new HotkeyGesture
        {
            Enabled = Enabled,
            Key = Key,
            Modifiers = NormalizeModifiers(Modifiers)
        };
    }

    public bool IsValid()
    {
        return !Enabled || (Key != VirtualKey.None && NormalizeModifiers(Modifiers) != KeyModifiers.None);
    }

    public bool ConflictsWith(HotkeyGesture? other)
    {
        if (other is null || !Enabled || !other.Enabled)
        {
            return false;
        }

        return Key == other.Key && NormalizeModifiers(Modifiers) == NormalizeModifiers(other.Modifiers);
    }

    public uint ToNativeModifiers()
    {
        var modifiers = NormalizeModifiers(Modifiers);
        uint nativeModifiers = NativeMethods.ModNorepeat;

        if ((modifiers & KeyModifiers.Control) != 0)
        {
            nativeModifiers |= NativeMethods.ModControl;
        }

        if ((modifiers & KeyModifiers.Alt) != 0)
        {
            nativeModifiers |= NativeMethods.ModAlt;
        }

        if ((modifiers & KeyModifiers.Shift) != 0)
        {
            nativeModifiers |= NativeMethods.ModShift;
        }

        return nativeModifiers;
    }

    public string ToDisplayString()
    {
        if (!Enabled || Key == VirtualKey.None)
        {
            return "(未设置)";
        }

        var parts = new List<string>();
        var modifiers = NormalizeModifiers(Modifiers);

        if ((modifiers & KeyModifiers.Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & KeyModifiers.Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((modifiers & KeyModifiers.Shift) != 0)
        {
            parts.Add("Shift");
        }

        parts.Add(GetKeyDisplayName(Key));
        return string.Join("+", parts);
    }

    private static KeyModifiers NormalizeModifiers(KeyModifiers modifiers)
    {
        return modifiers & AllowedModifiers;
    }

    private static string GetKeyDisplayName(VirtualKey key)
    {
        if (key is >= VirtualKey.D0 and <= VirtualKey.D9)
        {
            return ((char)('0' + (key - VirtualKey.D0))).ToString();
        }

        return key.ToString();
    }
}
