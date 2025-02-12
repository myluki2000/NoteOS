using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;
using System;

namespace NoteOS.EfiProtocols;

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_SIMPLE_FILE_SYSTEM_PROTOCOL {
    public readonly ulong Revision;
    public readonly delegate* unmanaged<EFI_SIMPLE_FILE_SYSTEM_PROTOCOL*, EFI_FILE_PROTOCOL**, EFI_STATUS> OpenVolume;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_FILE_PROTOCOL {
    public readonly ulong Revision;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_FILE_PROTOCOL**, char*, OpenMode, FileAttributes, EFI_STATUS> Open;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_STATUS> Close;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_STATUS> Delete;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, nuint*, void*, EFI_STATUS> Read;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, nuint*, void*, EFI_STATUS> Write;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, ulong*, EFI_STATUS> GetPosition;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, ulong, EFI_STATUS> SetPosition;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_GUID*, nuint*, void*, EFI_STATUS> GetInfo;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_GUID*, nuint, void*, EFI_STATUS> SetInfo;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_STATUS> Flush;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_FILE_PROTOCOL**, char*, OpenMode, FileAttributes, EFI_FILE_IO_TOKEN*, EFI_STATUS> OpenEx;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_FILE_IO_TOKEN*, EFI_STATUS> ReadEx;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_FILE_IO_TOKEN*, EFI_STATUS> WriteEx;
    public readonly delegate* unmanaged<EFI_FILE_PROTOCOL*, EFI_FILE_IO_TOKEN*, EFI_STATUS> FlushEx;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_FILE_IO_TOKEN {
    public readonly void* Event;
    public readonly EFI_STATUS Status;
    public readonly nuint BufferSize;
    public readonly void* Buffer;
}

public enum OpenMode : ulong {
    Read = 0x0000000000000001,
    Write = 0x0000000000000002,
    Create = 0x8000000000000000
}

[Flags]
public enum FileAttributes : ulong {
    None = 0x0000000000000000,
    ReadOnly = 0x0000000000000001,
    Hidden = 0x0000000000000002,
    System = 0x0000000000000004,
    Reserved = 0x0000000000000008,
    Directory = 0x0000000000000010,
    Archive = 0x00000000000000020,
    ValidAttr = 0x0000000000000037
}