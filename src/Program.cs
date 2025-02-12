using System;
using System.Threading;
using Internal.Runtime.CompilerHelpers;
using NoteOS.ExtensionMethods;
using NoteOS.EfiProtocols;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NoteOS;

struct UsableMemoryRegion
{
    public ulong Start;
    public ulong End;
    public nuint DescriptorIndex;
    public ulong Size => End - Start;
}

public unsafe static class Program
{

    public static void Main()
    {
        Console.WriteLine("Started os loader.");

        GraphicsOutputProtocol.Init();
        GraphicsOutputProtocol.SetMode();

        // get memory map
        Console.WriteLine("Getting memory map...");
        nuint mapKey = 0;
        nuint descriptorSize = 0;
        uint descriptorVersion = 0;

        EFI_STATUS memoryMapReturnCode = EFI_STATUS.EFI_BUFFER_TOO_SMALL;
        nuint memoryMapSize = 0;

        EfiSystemTable->BootServices->GetMemoryMap(&memoryMapSize, null, &mapKey, &descriptorSize, &descriptorVersion);
        EFI_MEMORY_DESCRIPTOR* memoryMap = stackalloc EFI_MEMORY_DESCRIPTOR[(int)(memoryMapSize / (nuint)sizeof(EFI_MEMORY_DESCRIPTOR)) + 2];
        memoryMapReturnCode = EfiSystemTable->BootServices->GetMemoryMap(&memoryMapSize, memoryMap, &mapKey, &descriptorSize, &descriptorVersion);

        if (memoryMapReturnCode != EFI_STATUS.EFI_SUCCESS)
        {
            Console.WriteLine("Memory map return code:");
            PrintEfiStatusCode(memoryMapReturnCode);
            while (true) { }
        }

        // exit boot services
        EFI_STATUS exitBootServicesReturnCode = EfiSystemTable->BootServices->ExitBootServices(EfiImageHandle, mapKey);

        if (exitBootServicesReturnCode != EFI_STATUS.EFI_SUCCESS)
        {
            Console.WriteLine("Exit boot services return code:");
            PrintEfiStatusCode(exitBootServicesReturnCode);
            while (true) { }
        }

        // find largest usable memory region
        UsableMemoryRegion* usableMemoryRegions = stackalloc UsableMemoryRegion[(int)(memoryMapSize / descriptorSize)];
        nuint usableMemoryRegionCount = 0;

        for (nuint i = 0; i < memoryMapSize / descriptorSize; i++)
        {
            EFI_MEMORY_DESCRIPTOR* descriptor = (EFI_MEMORY_DESCRIPTOR*)((byte*)memoryMap + (i * descriptorSize));

            if (!(descriptor->Type == EFI_MEMORY_TYPE.EfiConventionalMemory
                || descriptor->Type == EFI_MEMORY_TYPE.EfiBootServicesCode
                || descriptor->Type == EFI_MEMORY_TYPE.EfiBootServicesData))
                continue;


            usableMemoryRegions[usableMemoryRegionCount].Start = descriptor->PhysicalStart;
            usableMemoryRegions[usableMemoryRegionCount].End = descriptor->PhysicalStart + (descriptor->NumberOfPages * 4096);
            usableMemoryRegions[usableMemoryRegionCount].DescriptorIndex = i;
            usableMemoryRegionCount++;
        }

        for (nuint i = 0; i < memoryMapSize / descriptorSize; i++)
        {
            EFI_MEMORY_DESCRIPTOR* descriptor = (EFI_MEMORY_DESCRIPTOR*)((byte*)memoryMap + (i * descriptorSize));

            ulong startAddress = descriptor->PhysicalStart;
            ulong endAddress = descriptor->PhysicalStart + (descriptor->NumberOfPages * 4096);

            for (nuint j = 0; j < usableMemoryRegionCount; j++)
            {
                UsableMemoryRegion* usableMemoryRegion = &usableMemoryRegions[j];
                if (usableMemoryRegion->DescriptorIndex == i)
                    continue;

                if (startAddress >= usableMemoryRegion->Start && startAddress < usableMemoryRegion->End)
                    usableMemoryRegion->End = startAddress;
                else if (endAddress > usableMemoryRegion->Start && endAddress <= usableMemoryRegion->End)
                    usableMemoryRegion->Start = endAddress;
            }
        }

        nuint largestRegion = 0;
        for (nuint i = 0; i < usableMemoryRegionCount; i++)
        {
            UsableMemoryRegion* usableMemoryRegion = &usableMemoryRegions[i];
            if (usableMemoryRegion->Size > usableMemoryRegions[largestRegion].Size)
            {
                largestRegion = i;
            }
        }

        // give the memory to our allocator
        StartupCodeHelpers.AllocatorStartAddress = (nuint)usableMemoryRegions[largestRegion].Start;
        StartupCodeHelpers.AllocatorEndAddress = (nuint)usableMemoryRegions[largestRegion].End;
        StartupCodeHelpers.UseUefiAllocator = false;





        

        //Console.WriteLine("PCI Devices: ");
        Pci.PciDevice* devices = stackalloc Pci.PciDevice[32];
        int pciDeviceCount = Pci.GetDevices(devices);

        bool hasAhciController = false;
        AhciController? ahciController = null;
        for (int i = 0; i < pciDeviceCount; i++)
        {
            Pci.PciDevice device = devices[i];
            //Console.Write("Bus: ");
            //Console.Write(device.Bus);
            //Console.Write(", Slot: ");
            //Console.Write(device.Slot);
            //Console.Write(", Vendor: ");
            //Console.Write(((nuint)device.VendorId).ToString(true));
            //Console.Write(", Device: ");
            //Console.Write(((nuint)device.DeviceId).ToString(true));
            //Console.Write(", Class: ");
            //Console.Write(device.ClassId);
            //Console.Write(", Subclass: ");
            //Console.Write(device.SubclassId);
            //Console.Write(", Prog. Interface: ");
            //Console.Write(device.ProgrammingInterface);
            //Console.Write(", Status: ");
            //Console.WriteLine(((nuint)device.Status).ToString(true));

            if (!hasAhciController && device.ClassId == 1 && device.SubclassId == 6 && device.ProgrammingInterface == 1)
            {
                ahciController = new AhciController(device);
                hasAhciController = true;
            }
        }

        if (!ahciController.HasValue)
        {
            //Console.WriteLine("No AHCI controller found.");
            while (true) { }
        }

        //Console.Write("Port Implemented:");
        //Console.WriteLine(ahciController.AhciBaseMemoryPtr->PortImplemented.ToString(true));

        //Console.WriteLine("Ports:");
        int foundDisks = 0;
        for (int i = 0; i < 32; i++)
        {
            if (ahciController.Value.AhciBaseMemoryPtr->Ports[i].HasDevice)
            {
                //Console.Write("Port ");
                //Console.Write(i.ToString());
                //Console.WriteLine(" has device. ");

                //Console.WriteLine("Port rebase...");
                ahciController.Value.AhciBaseMemoryPtr->Ports[i].Rebase((nuint)(0x400000 + 0x100000 * foundDisks));

                /*Console.WriteLine("AHCI Controller read...");
                byte[] buffer = new byte[100 * 512];
                for(int j = 0; j < buffer.Length; j++) {
                    buffer[j] = 0;
                }
                fixed(byte* bufferPtr = &buffer[0]) {
                    bool result = ahciController.AhciBaseMemoryPtr->Ports[i].Read(0x0, 0x0, 100, (ushort*)bufferPtr);
                    Console.Write("Result:");
                    for(int j = 0; j < buffer.Length; j++) {
                        byte* valuePtr = bufferPtr + j;
                        uint value = *(uint*)valuePtr;
                        if(*valuePtr == 0xDE) {
                            Console.Write(value.ToString(true));
                            Console.Write(", ");
                        }
                    }
                    Console.WriteLine("");
                }*/

                foundDisks++;
            }
        }

        // try to write to disk 1
        ushort* buffer = stackalloc ushort[2];
        buffer[0] = 0xDEAD;
        buffer[1] = 0xBEEF;

        //Console.WriteLine("AHCI Controller write...");
        bool result = ahciController.Value.AhciBaseMemoryPtr->Ports[1].Write(0x0, 0x0, 2, buffer);
        //Console.Write("Result:");
        //Console.Write(result.ToString());
        //Console.WriteLine("");

        if (result)
        {
            DrawRectangle(0, 0, 0x0000FF00);
        }

        // try to read from disk 1
        ushort* buffer2 = stackalloc ushort[2];
        buffer2[0] = 0;
        buffer2[1] = 0;

        Console.Enabled = false;

        //Console.WriteLine("AHCI Controller read...");
        result = ahciController.Value.AhciBaseMemoryPtr->Ports[1].Read(0x0, 0x0, 2, buffer2);

        //Console.Write("Result:");
        //Console.Write(result.ToString());
        //Console.Write(", ");
        //Console.Write("Buffer:");
        //Console.Write(buffer2[0].ToString(true));
        //Console.Write(", ");
        //Console.WriteLine(buffer2[1].ToString(true));

        GraphicsOutputProtocol.PrintLine("AHCI Controller read...");
        GraphicsOutputProtocol.Print("Result:");
        GraphicsOutputProtocol.Print(result.ToString());
        GraphicsOutputProtocol.Print(", ");
        GraphicsOutputProtocol.Print("Buffer:");
        GraphicsOutputProtocol.Print(buffer2[0].ToString(true));
        GraphicsOutputProtocol.Print(", ");
        GraphicsOutputProtocol.Print(buffer2[1].ToString(true));


        while (true) { }
    }

    public static void DrawRectangle(int x, int y, uint color)
    {
        for (int iy = y; iy < y + 100; iy++)
        {
            for (int ix = x; ix < x + 100; ix++)
            {
                GraphicsOutputProtocol.PlotPixel32bpp(ix, iy, color);
            }
        }
    }

    public static void PrintEfiStatusCode(EFI_STATUS statusCode)
    {
        string resultCodeString = ((nuint)statusCode).ToString(true);
        Console.WriteLine(resultCodeString);
        object.Free(resultCodeString);
    }
}