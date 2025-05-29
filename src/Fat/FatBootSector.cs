using System.Runtime.InteropServices;

namespace NoteOS.Fat;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct FatBootSector {
    public fixed byte BootJmp[3];
    public fixed byte OemName[8];
    public ushort BytesPerSector;
    public byte SectorsPerCluster;
    public ushort ReservedSectorCount;
    /// <summary>
    /// How many copies of the FAT table are stored on the disk.
    /// </summary>
    public byte TableCount;
    public ushort RootEntryCount;
    public ushort TotalSectors16;
    public byte Media;
    public ushort TableSize16;
    public ushort SectorsPerTrack;
    public ushort HeadSideCount;
    public uint HiddenSectorCount;
    public uint TotalSectors32;

    public fixed byte ExtendedSection[54];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Fat32ExtendedBootSector {
    public uint TableSize32;
    public ushort ExtendedFlags;
    public ushort FatVersion;
    public uint RootCluster;
    public ushort FatInfo;
    public ushort BackupBootSector;
    public fixed byte Reserved[12];
    public byte DriveNumber;
    public byte Reserved1;
    public byte BootSignature;
    public uint VolumeId;
    public fixed byte VolumeLabel[11];
    public fixed byte FileSystemType[8];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Fat16ExtendedBootSector {
    public byte PhysicalDriveNumber;
    public byte Reserved1;
    public byte BootSignature;
    public uint VolumeId;
    public fixed byte VolumeLabel[11];
    public fixed byte FatTypeLabel[8];
}


