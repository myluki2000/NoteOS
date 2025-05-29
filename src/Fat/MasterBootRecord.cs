using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NoteOS.Fat;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MasterBootRecord {
    public fixed byte BootCode[440];
    public uint DiskSignature;
    public ushort Reserved;
    public MbrPartitionTableEntryInlineArray PartitionTableEntries;
    public ushort BootSignature;
}

[InlineArray(4)]
public struct MbrPartitionTableEntryInlineArray {
    public MbrPartitionTableEntry Element;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MbrPartitionTableEntry {
    public byte DriveAttributes;
    public fixed byte ChsStartAddress[3];
    public byte PartitionType;
    public fixed byte ChsEndAddress[3];
    public uint LbaStartAddress;
    public uint SizeInSectors;

    public bool HasPartition => PartitionType != 0;
}