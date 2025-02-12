using System.Runtime.InteropServices;
using Internal.Runtime.CompilerHelpers;

namespace NoteOS.EfiProtocols;

public static unsafe class LoadFileProtocol {
    public static EFI_LOAD_FILE_PROTOCOL* loadFile;

    public static void Init() {
        loadFile = (EFI_LOAD_FILE_PROTOCOL*)EfiProtocol.Get(EfiProtocol.GUID_LOAD_FILE_PROTOCOL);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_LOAD_FILE_PROTOCOL {
    public readonly delegate* unmanaged<EFI_LOAD_FILE_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*, bool, nuint*, void*, EFI_STATUS> LoadFile;
}