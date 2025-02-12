using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;

namespace NoteOS.EfiProtocols;

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_LOADED_IMAGE_PROTOCOL {
    public readonly uint Revision;
    public readonly EFI_HANDLE ParentHandle;
    public readonly EFI_SYSTEM_TABLE* SystemTable;
    public readonly EFI_HANDLE DeviceHandle;
    public readonly EFI_DEVICE_PATH_PROTOCOL* FilePath;
    private readonly void* reserved;
    public readonly uint LoadOptionsSize;
    public readonly void* LoadOptions;
    public readonly void* ImageBase;
    public readonly ulong ImageSize;
    public readonly EFI_MEMORY_TYPE ImageCodeType;
    public readonly EFI_MEMORY_TYPE ImageDataType;
    public readonly delegate* unmanaged<EFI_HANDLE, EFI_STATUS> Unload;
}