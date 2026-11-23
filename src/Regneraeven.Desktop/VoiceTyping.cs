using System.Runtime.InteropServices;

namespace Regneraeven.Desktop;

/// <summary>
/// Opens Windows' built-in voice typing (the same as pressing Win + H). It understands Danish and types
/// into whatever field has focus – the page focuses the problem text box before asking for this.
/// Windows' own speech API on most PCs only knows English, so voice typing is the reliable way to get Danish.
/// </summary>
internal static class VoiceTyping
{
    private const int InputKeyboard = 1;
    private const uint KeyUp = 0x0002;
    private const ushort VkLeftWin = 0x5B;
    private const ushort VkH = 0x48;

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    // INPUT is a union; the keyboard variant is padded to the size of the mouse variant.
    [StructLayout(LayoutKind.Explicit)]
    private struct Input
    {
        [FieldOffset(0)] public int Type;
        [FieldOffset(8)] public KeyboardInput Keyboard;
        [FieldOffset(8)] private MousePadding _padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MousePadding
    {
        public int Dx, Dy;
        public uint MouseData, Flags, Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    private static Input Key(ushort vk, bool up) => new()
    {
        Type = InputKeyboard,
        Keyboard = new KeyboardInput { Vk = vk, Flags = up ? KeyUp : 0 },
    };

    public static bool Start()
    {
        Input[] keys = [Key(VkLeftWin, false), Key(VkH, false), Key(VkH, true), Key(VkLeftWin, true)];
        return SendInput((uint)keys.Length, keys, Marshal.SizeOf<Input>()) == keys.Length;
    }
}
