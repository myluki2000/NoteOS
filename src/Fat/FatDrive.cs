using System;
using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using NoteOS;
using NoteOS.EfiProtocols;
using NoteOS.Ahci;
using NoteOS.ExtensionMethods;

namespace NoteOS.Fat;

public unsafe struct FatDrive
{
    private HbaPort* port;

    private MasterBootRecord mbr;

    public FatVolumeInlineArray Volumes;

    public FatDrive(HbaPort* port)
    {
        this.port = port;

        byte* firstSectorBytes = stackalloc byte[512];
        bool result = port->Read(0x0, 0x0, 1, (byte*)firstSectorBytes);
        Debug.Assert(result, "Failed to read first sector of disk.");

        // check if first sector is MBR or VBR
        if (firstSectorBytes[0] == 0xEB && firstSectorBytes[1] == 0x3C && firstSectorBytes[2] == 0x90)
        {
            Debug.WriteLine("First sector is VBR.");
            Volumes[0] = new FatVolume(port, new MbrPartitionTableEntry { LbaStartAddress = 0x0 });
        }
        else
        {
            Debug.WriteLine("First sector is MBR.");
            fixed (MasterBootRecord* mbrPtr = &this.mbr)
            {
                Debug.WriteLine("Reading MBR...");
                result = port->Read(0x0, 0x0, 1, (byte*)mbrPtr);
                Debug.Assert(result, "Failed to read MBR of disk.");
            }
            for (int i = 0; i < 4; i++)
            {
                if (mbr.PartitionTableEntries[i].HasPartition)
                {
                    Volumes[i] = new FatVolume(port, mbr.PartitionTableEntries[i]);
                }
            }
        }
    }
}

[InlineArray(4)]
public struct FatVolumeInlineArray
{
    FatVolume? Element;
}

public unsafe struct FatVolume
{

    private HbaPort* port;
    private MbrPartitionTableEntry partitionTableEntry;

    private FatBootSector bootSector;

    /// <summary>
    /// Total sectors in volume (including VBR).
    /// </summary>
    public uint TotalSectors => (bootSector.TotalSectors16 == 0) ? bootSector.TotalSectors32 : bootSector.TotalSectors16;

    /// <summary>
    /// FAT size in sectors.
    /// </summary>
    public uint FatSize
    {
        get
        {
            fixed (FatBootSector* bsPtr = &bootSector)
            {
                return (bootSector.TableSize16 == 0)
                    ? ((Fat32ExtendedBootSector*)bsPtr->ExtendedSection)->TableSize32
                    : bootSector.TableSize16;
            }
        }
    }

    /// <summary>
    /// The size of the root directory (unless volume is FAT32, in which case the size will be 0).
    /// This calculation will round up. 32 is the size of a FAT directory in bytes. 
    /// </summary>
    public uint RootDirectorySectorCount => (((uint)bootSector.RootEntryCount * 32 + bootSector.BytesPerSector - 1) / bootSector.BytesPerSector);

    /// <summary>
    /// The first data sector (that is, the first sector in which directories and files may be stored)
    /// </summary>
    public uint FirstDataSector => bootSector.ReservedSectorCount + bootSector.TableCount * FatSize + RootDirectorySectorCount;

    /// <summary>
    /// The first sector in the File Allocation Table.
    /// </summary>
    public uint FirstFatSector => bootSector.ReservedSectorCount;

    /// <summary>
    /// The total number of data sectors.
    /// </summary>
    public uint DataSectorCount => TotalSectors - (bootSector.ReservedSectorCount + (bootSector.TableCount * FatSize) + RootDirectorySectorCount);

    /// <summary>
    /// The total number of clusters.
    /// This rounds down.
    /// </summary>
    public uint TotalClusterCount => DataSectorCount / bootSector.SectorsPerCluster;
    public uint BytesPerCluster => (uint)bootSector.SectorsPerCluster * bootSector.BytesPerSector;

    public FatType FatType
    {
        get
        {
            if (bootSector.BytesPerSector == 0)
                return FatType.EXFAT;
            else if (TotalClusterCount < 4085)
                return FatType.FAT12;
            else if (TotalClusterCount < 65525)
                return FatType.FAT16;
            else
                return FatType.FAT32;
        }
    }

    public FatVolume(HbaPort* port, MbrPartitionTableEntry partitionTableEntry)
    {
        this.port = port;
        this.partitionTableEntry = partitionTableEntry;

        byte* bootSectorBytes = stackalloc byte[512];

        Debug.WriteLine("Reading VBR of partition...");
        bool result = port->Read(partitionTableEntry.LbaStartAddress, 0x0, 1, bootSectorBytes);

        Debug.Assert(result, "Failed to read VBR of partition.");
        fixed (FatBootSector* bootSectorPtr = &bootSector)
        {
            Unsafe.Memcpy((void*)bootSectorPtr, bootSectorBytes, (uint)sizeof(FatBootSector));
        }

        if (FatType != FatType.FAT32)
        {
            Debug.Fail("Only FAT32 volumes are supported.");
        }
    }

    public DirectoryEnumerator EnumerateRootDirectory()
    {
        uint rootCluster32 = 0;
        fixed (FatBootSector* bsPtr = &bootSector)
        {
            rootCluster32 = ((Fat32ExtendedBootSector*)bsPtr->ExtendedSection)->RootCluster;
        }

        ref FatVolume self = ref this;
        fixed(FatVolume* thisPtr = &self)
        {
            GraphicsOutputProtocol.PrintLine(BytesPerCluster.ToString(true));
            Debug.Write("Root cluster: ");
            Debug.WriteLine(rootCluster32, true);
            DirectoryEnumerator result = new DirectoryEnumerator(thisPtr, rootCluster32);
            return result;
        }
    }

    public void ReadRootDirectory()
    {
        uint rootCluster32;
        fixed (FatBootSector* bsPtr = &bootSector)
        {
            rootCluster32 = ((Fat32ExtendedBootSector*)bsPtr->ExtendedSection)->RootCluster;
        }
        ReadDirectory(rootCluster32);
    }

    public void ReadDirectory(uint cluster)
    {
        byte* clusterBytes = stackalloc byte[(int)BytesPerCluster];
        Debug.WriteLine("Reading directory...");

        FatDirectoryEntry* dirEntryPtr = (FatDirectoryEntry*)clusterBytes;

        do
        {
            bool result = ReadCluster(cluster, clusterBytes);
            Debug.Assert(result, "Failed to read directory sectors.");

            for (int i = 0; i < BytesPerCluster; i += sizeof(FatDirectoryEntry))
            {
                // if first byte is 0x0, then we have reached the end of the directory
                if (*(byte*)dirEntryPtr == 0x0)
                {
                    dirEntryPtr++;
                    Debug.WriteLine("End of directory.");
                    goto end_iteration;
                }

                // if first byte is 0xE5, then the current entry is unused
                if (*(byte*)dirEntryPtr == 0xE5)
                {
                    dirEntryPtr++;
                    continue;
                }

                Debug.Write("File name: ");
                //Debug.WriteLine(new string(dirEntryPtr->FileName, 11));
                dirEntryPtr++;
            }
            Debug.WriteLine("End of cluster.");
            cluster = NextClusterAddress(cluster);
        } while (*((byte*)(dirEntryPtr - 1)) != 0x0);
    end_iteration:
        { }
    }

    public bool ReadCluster(uint cluster, byte* buffer)
    {
        Debug.Write("Reading cluster: ");
        Debug.WriteLine(cluster, true);
        ulong sector = FirstSectorOfCluster(cluster);
        Debug.Write("Sector: ");
        Debug.WriteLine(sector, true);
        Debug.Write("Sectors per cluster: ");
        Debug.WriteLine(bootSector.SectorsPerCluster, true);
        bool result = port->Read((uint)sector, (uint)(sector >> 32), bootSector.SectorsPerCluster, buffer);
        if(result) {
            Debug.WriteLine("Read cluster successfully.");
        } else {
            Debug.WriteLine("Failed to read cluster.");
            Debug.Fail("Failed to read cluster.");
        }
        return result;
    }

    public ulong FirstSectorOfCluster(uint cluster)
    {
        return ((ulong)(cluster - 2) * bootSector.SectorsPerCluster) + FirstDataSector;
    }

    public uint NextClusterAddress(uint cluster)
    {
        if (FatType != FatType.FAT16 && FatType != FatType.FAT32)
        {
            Debug.Fail("Only FAT16 and FAT32 are supported.");
        }

        uint fatOffset = cluster * (uint)(FatType switch
        {
            FatType.FAT16 => 2,
            FatType.FAT32 => 4,
            _ => 0
        });
        uint sector = FirstFatSector + (fatOffset / bootSector.BytesPerSector);
        uint offset = fatOffset % bootSector.BytesPerSector;

        byte* fatSectorBytes = stackalloc byte[bootSector.BytesPerSector];
        bool result = port->Read(sector, 0, 1, fatSectorBytes);
        Debug.Assert(result, "Failed to read FAT sector.");

        uint nextCluster = *(uint*)fatSectorBytes[offset];
        if (FatType == FatType.FAT16)
        {
            return nextCluster & 0xFFFF;
        }
        else if (FatType == FatType.FAT32)
        {
            return nextCluster & 0x0FFFFFFF;
        }

        Debug.Fail("Only FAT16 and FAT32 are supported.");
        return 0x0;
    }
}

public unsafe struct DirectoryEnumerator
{
    private uint firstCluster;
    private uint currentCluster;
    private uint currentEntryInCluster;

    private FatVolume* volume;

    public DirectoryEnumerator(FatVolume* volume, uint firstCluster)
    {
        this.firstCluster = firstCluster;
        this.currentCluster = firstCluster;
        this.volume = volume;
    }

    public FatDirectoryEntry Current;

    public bool MoveNext()
    {
        uint bytesPerCluster = (uint)volume->BytesPerCluster;

        byte* clusterBytes = stackalloc byte[(int)volume->BytesPerCluster];
        bool result = volume->ReadCluster(currentCluster, clusterBytes);
        Debug.Assert(result, "Failed to read directory sectors.");
        fixed (FatDirectoryEntry* currentPtr = &Current)
        {
            Unsafe.Memcpy(currentPtr, clusterBytes + currentEntryInCluster * sizeof(FatDirectoryEntry), (uint)sizeof(FatDirectoryEntry));
            if (*(byte*)currentPtr == 0x0)
            {
                Debug.WriteLine("End of directory.");
                return false; // End of directory
            }
        }

        currentEntryInCluster++;
        if (currentEntryInCluster >= volume->BytesPerCluster / sizeof(FatDirectoryEntry))
        {
            currentCluster = volume->NextClusterAddress(currentCluster);
            currentEntryInCluster = 0;
        }

        return true;
    }

    public void Reset()
    {
        currentCluster = firstCluster;
        currentEntryInCluster = 0;
    }
}

public enum FatType
{
    FAT12,
    FAT16,
    FAT32,
    EXFAT
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct FatDirectoryEntry
{
    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
    public fixed byte FileName[11];
    public byte Attributes;
    public byte Reserved;
    /// <summary>
    /// Creation time in hundredths of a second, although the official FAT Specification from Microsoft says it is tenths of a second. 
    /// Range 0-199 inclusive. Based on simple tests, Ubuntu16.10 stores either 0 or 100 while Windows7 stores 0-199 in this field. 
    /// </summary>
    public byte CreationTimeTenths;
    /// <summary>
    /// The time that the file was created. Multiply Seconds by 2. 
    /// Hour: 5 bits
    /// Minute: 6 bits
    /// Second: 5 bits
    /// </summary>
    public ushort CreationTime;
    /// <summary>
    /// The date that the file was created.
    /// Year: 7 bits
    /// Month: 4 bits
    /// Day: 5 bits
    /// </summary>
    public ushort CreationDate;
    /// <summary>
    /// The date that the file was last accessed at.
    /// Year: 7 bits
    /// Month: 4 bits
    /// Day: 5 bits
    /// </summary>
    public ushort LastAccessedDate;
    /// <summary>
    /// The high 16 bits of this entry's first cluster number. For FAT 12 and FAT 16 this is always zero. 
    /// </summary>
    public ushort FirstClusterHigh;
    /// <summary>
    /// The time that the file was last modified at. Multiply Seconds by 2. 
    /// Hour: 5 bits
    /// Minute: 6 bits
    /// Second: 5 bits
    /// </summary>
    public ushort LastModifiedTime;
    /// <summary>
    /// The date that the file was last modified at.
    /// Year: 7 bits
    /// Month: 4 bits
    /// Day: 5 bits
    /// </summary>
    public ushort LastModifiedDate;
    /// <summary>
    /// The low 16 bits of this entry's first cluster number. Use this number to find the first cluster for this entry. 
    /// </summary>
    public ushort FirstClusterLow;
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public uint FileSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct FatLfnEntry
{
    public byte SequenceNumber;
    public fixed ushort Name1[5];
    public byte Attributes;
    public byte Type;
    public byte Checksum;
    public fixed ushort Name2[6];
    public ushort FirstCluster;
    public fixed ushort Name3[2];
}