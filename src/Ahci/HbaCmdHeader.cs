using System.Runtime.InteropServices;

namespace NoteOS.Ahci;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct HbaCmdHeader
{
    /// <summary>
    /// Bits 0-4: Command FIS length in DWORDS, 2 ~ 16
    /// Bit 5: ATAPI
    /// Bit 6: Write, 1: H2D, 0: D2H
    /// Bit 7: Prefetchable
    /// </summary>
    public byte Byte0;
    /// <summary>
    /// Bit 0: Reset
    /// Bit 1: BIST
    /// Bit 2: Clear busy upon R_OK
    /// Bit 3: Reserved
    /// Bit 4-7: Port multiplier port
    /// </summary>
    public byte Byte1;
    /// <summary>
    /// Physical Region Descriptor Table length in entries.
    /// </summary>
    public short PhysicalRegionDescriptorTableLength;
    public uint PhysicalRegionDescriptorByteCountTransferred;
    public nuint CommandTableDescriptorBaseAddress;
    public fixed uint Reserved[4];
}