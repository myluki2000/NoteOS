using System.Runtime.InteropServices;

namespace NoteOS.Ahci;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct HbaPrdtEntry
{
    public uint DataBaseAddress;
    public uint DataBaseAddressUpper;
    public uint Reserved0;

    /// <summary>
    /// Byte 0-21: Byte count, 4M max
    /// Byte 22-30: Reserved
    /// Bit 31: Interrupt on completion
    /// </summary>
    public uint DoubleWord3;
}