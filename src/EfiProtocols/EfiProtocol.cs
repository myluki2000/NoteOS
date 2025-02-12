using Internal.Runtime.CompilerHelpers;
using System;

namespace NoteOS.EfiProtocols;

public static unsafe class EfiProtocol {
    public static void* Get(EFI_GUID protocolId) {
        void* protocol;

        EFI_STATUS locateProtocolReturnCode = EfiSystemTable->BootServices->LocateProtocol(&protocolId, null, &protocol);

        if(locateProtocolReturnCode != EFI_STATUS.EFI_SUCCESS) {
            Console.Write("Failed to locate protocol! Return code:");
            Program.PrintEfiStatusCode(locateProtocolReturnCode);
            while(true) { }
        }

        return protocol;
    }

    public static void* GetOfHandle(EFI_HANDLE handle, EFI_GUID protocolId) {
        void* protocol;

        EFI_STATUS handleProtocolReturnCode = EfiSystemTable->BootServices->HandleProtocol(handle, &protocolId, &protocol);

        return protocol;
    }

    public static readonly EFI_GUID GUID_LOADED_IMAGE_PROTOCOL = new EFI_GUID(0x5B1B31A1, 0x9562, 0x11d2, 0x8E, 0x3F, 0x00, 0xA0, 0xC9, 0x69, 0x72, 0x3B);
    public static readonly EFI_GUID GUID_GOP_PROTOCOL = new EFI_GUID(0x9042a9de, 0x23dc, 0x4a38, 0x96, 0xfb, 0x7a, 0xde, 0xd0, 0x80, 0x51, 0x6a);
    public static readonly EFI_GUID GUID_LOAD_FILE_PROTOCOL = new EFI_GUID(0x56EC3091, 0x954C, 0x11d2, 0x8e, 0x3f, 0x00, 0xa0, 0xc9, 0x69, 0x72, 0x3b);
    public static readonly EFI_GUID GUID_LOAD_FILE2_PROTOCOL = new EFI_GUID(0x4006c0c1, 0xfcb3, 0x403e, 0x99, 0x6d, 0x4a, 0x6c, 0x87, 0x24, 0xe0, 0x6d);
    public static readonly EFI_GUID GUID_DEVICE_PATH_PROTOCOL = new EFI_GUID(0x09576e91, 0x6d3f, 0x11d2, 0x8e, 0x39, 0x00, 0xa0, 0xc9, 0x69, 0x72, 0x3b);
    public static readonly EFI_GUID GUID_DEVICE_PATH_UTILITIES_PROTOCOL = new EFI_GUID(0x379be4e, 0xd706, 0x437d, 0xb0, 0x37, 0xed, 0xb8, 0x2f, 0xb7, 0x72, 0xa4);
    public static readonly EFI_GUID GUID_SIMPLE_FILE_SYSTEM_PROTOCOL = new EFI_GUID(0x0964e5b22, 0x6459, 0x11d2, 0x8e, 0x39, 0x00, 0xa0, 0xc9, 0x69, 0x72, 0x3b);
}