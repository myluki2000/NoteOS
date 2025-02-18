using System.Runtime.InteropServices;

namespace NoteOS.Ahci;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct FisRegH2d
{
    /// <summary>
    /// FIS_TYPE_REG_H2D
    /// </summary>
    public byte FisType;
    /// <summary>
    /// Bit 0-3: Port multiplier
    /// Bit 4-6: Reserved
    /// Bit 7: Command, 1: Command, 0: Control
    /// </summary>
    public byte Byte1;
    /// <summary>
    /// Command register
    /// </summary>
    public byte Command;
    /// <summary>
    /// Features register, 7:0
    /// </summary>
    public byte FeatureLow;

    public byte LbaLow;
    public byte LbaMid;
    public byte LbaHigh;
    public byte Device;

    public byte LbaLowExp;
    public byte LbaMidExp;
    public byte LbaHighExp;
    public byte FeatureHigh;

    public ushort Count;

    public byte IsochronousCommandCompletion;
    public byte Control;

    public fixed byte Reserved[4];
}