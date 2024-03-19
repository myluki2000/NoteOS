using System;
using System.Threading;
using Internal.Runtime.CompilerHelpers;
using NoteOS.ExtensionMethods;

namespace NoteOS;

public unsafe static class Program {

    public static void Main() {
        Console.WriteLine("Started os loader.");

        Console.WriteLine("Getting memory map...");

        // load GOP
        EFI_GUID gopGuid = new EFI_GUID(0x9042a9de, 0x23dc, 0x4a38, 0x96, 0xfb, 0x7a, 0xde, 0xd0, 0x80, 0x51, 0x6a);
        EFI_GRAPHICS_OUTPUT_PROTOCOL* gop;
        EFI_STATUS locateGopReturnCode = EfiSystemTable->BootServices->LocateProtocol(&gopGuid, null, (void**)&gop);
        
        if(locateGopReturnCode != EFI_STATUS.EFI_SUCCESS) {
            Console.Write("Failed to locate GOP protocol! Return code:");
            PrintEfiStatusCode(locateGopReturnCode);
            while(true) { }
        }

        // get current mode
        nuint sizeOfInfo;
        EFI_GRAPHICS_OUTPUT_MODE_INFORMATION* info;
        EFI_STATUS gopQueryModeReturnCode = gop->QueryMode(gop,
                                                           gop->Mode == null ? 0 : gop->Mode->Mode, 
                                                           &sizeOfInfo,
                                                           &info);
        
        if(gopQueryModeReturnCode != EFI_STATUS.EFI_SUCCESS) {
            Console.Write("Failed to query GOP mode! Return code:");
            PrintEfiStatusCode(gopQueryModeReturnCode);
            while(true) { }
            // TODO: If status code is EFI_NOT_STARTED, then the GOP is not started and we need to start it
            // see here: https://wiki.osdev.org/GOP#Get_the_Current_Mode
        }

        uint numModes = gop->Mode->MaxMode;
        uint nativeMode = gop->Mode->Mode;

        // query available modes
        for(uint i = 0; i < numModes; i++) {
            EFI_STATUS queryModeReturnCode = gop->QueryMode(gop, i, &sizeOfInfo, &info);
            if(queryModeReturnCode != EFI_STATUS.EFI_SUCCESS) {
                Console.Write("Failed to query GOP mode! Return code:");
                PrintEfiStatusCode(queryModeReturnCode);
                while(true) { }
            }

            Console.Write("Mode ");
            Console.Write((int)i);
            Console.Write(": ");
            if(i == nativeMode)
                Console.Write("(native) ");
            Console.Write((int)info->HorizontalResolution);
            Console.Write("x");
            Console.Write((int)info->VerticalResolution);
            Console.Write(" PixelsPerScanLine: ");
            Console.Write((int)info->PixelsPerScanLine);
            Console.Write(" PixelFormat: ");
            Console.WriteLine((int)info->PixelFormat);
        }

        // set video mode
        EFI_STATUS setModeReturnCode = gop->SetMode(gop, nativeMode);
        if(setModeReturnCode != EFI_STATUS.EFI_SUCCESS) {
            Console.Write("Failed to set GOP mode! Return code:");
            PrintEfiStatusCode(setModeReturnCode);
            while(true) { }
        }

        void PlotPixel32bpp(EFI_GRAPHICS_OUTPUT_PROTOCOL* gop, int x, int y, uint pixel) {
            uint* pixelAddress = (uint*)((long)gop->Mode->FrameBufferBase + 4 * gop->Mode->Info->PixelsPerScanLine * y + 4 * x);
            *pixelAddress = pixel;
        }

        // get memory map
        nuint mapKey = 0;
        nuint descriptorSize = 0;
        uint descriptorVersion = 0;

        EFI_STATUS memoryMapReturnCode = EFI_STATUS.EFI_BUFFER_TOO_SMALL;
        nuint memoryMapSize = 1;

        while(memoryMapReturnCode == EFI_STATUS.EFI_BUFFER_TOO_SMALL) {
            memoryMapSize *= 2;
            EFI_MEMORY_DESCRIPTOR* memoryMap = stackalloc EFI_MEMORY_DESCRIPTOR[(int)memoryMapSize];

            memoryMapReturnCode = EfiSystemTable->BootServices->GetMemoryMap(&memoryMapSize, memoryMap, &mapKey, &descriptorSize, &descriptorVersion);
        }
        // If we print this, ExitBootServices will fail
        /* Console.WriteLine("Memory map return code:");
        string resultCodeString = ((nuint)memoryMapReturnCode).ToString(true);
        Console.WriteLine(resultCodeString);
        object.Free(resultCodeString); */

        // exit boot services
        EFI_STATUS exitBootServicesReturnCode = EfiSystemTable->BootServices->ExitBootServices(EfiImageHandle, mapKey);

        if(exitBootServicesReturnCode != EFI_STATUS.EFI_SUCCESS) {
            Console.WriteLine("Exit boot services return code:");
            PrintEfiStatusCode(exitBootServicesReturnCode);
            while(true) { }
        }

        // draw a red square
        for(int y = 0; y < 100; y++) {
            for(int x = 0; x < 100; x++) {
                PlotPixel32bpp(gop, x, y, 0x00FF0000);
            }
        }

        
    }

    private static unsafe void PrintEfiStatusCode(EFI_STATUS statusCode) {
        string resultCodeString = ((nuint)statusCode).ToString(true);
        Console.WriteLine(resultCodeString);
        object.Free(resultCodeString);
    }
}