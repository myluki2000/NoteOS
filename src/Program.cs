using System;
using Internal.Runtime.CompilerHelpers;
using NoteOS.ExtensionMethods;
using NoteOS.EfiProtocols;
using NoteOS.Ahci;
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

        Debug.WriteLine("Exited boot services.");

        Console.Enabled = false;

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

            if (!hasAhciController && device.ClassId == 1 && device.SubclassId == 6 && device.ProgrammingInterface == 1)
            {
                GraphicsOutputProtocol.PrintLine("AHCI Controller found.");
                ahciController = new AhciController(device);
                hasAhciController = true;
            }
        }

        if (!ahciController.HasValue)
        {
            GraphicsOutputProtocol.PrintLine("No AHCI controller found.");
            while (true) { }
        }

        int foundDisks = 0;
        for (int i = 0; i < 32; i++)
        {
            if (ahciController.Value.AhciBaseMemoryPtr->Ports[i].HasDevice)
            {
                Debug.Write("Port ");
                Debug.Write(i.ToString());
                Debug.WriteLine(" has device. Rebasing...");
                ahciController.Value.AhciBaseMemoryPtr->Ports[i].Rebase((nuint)(0x400000 + 0x100000 * foundDisks));
                Debug.WriteLine("Rebased.");

                foundDisks++;
            }
        }

        GraphicsOutputProtocol.Print("Found disks:");
        GraphicsOutputProtocol.PrintLine(foundDisks.ToString());

        // try to write to disk 1
        ushort[] buffer = new ushort[256];
        buffer[0] = 0xDEAD;
        buffer[1] = 0xBEEF;

        GraphicsOutputProtocol.PrintLine("AHCI Controller write...");
        fixed(ushort* bufferPtr = &buffer[0]) {
            Debug.WriteLine("Writing to disk 1...");
            bool result = ahciController.Value.AhciBaseMemoryPtr->Ports[1].Write(0x0, 0x0, 1, bufferPtr);
            Debug.WriteLine("Write complete.");
            GraphicsOutputProtocol.Print("Result:");
            GraphicsOutputProtocol.PrintLine(result.ToString());
        }

        // try to read from disk 1
        ushort[] buffer2 = new ushort[256];
        for (int i = 0; i < buffer2.Length; i++)
        {
            buffer2[i] = 0;
        }

        GraphicsOutputProtocol.PrintLine("AHCI Controller read...");
        fixed(ushort* buffer2Ptr = &buffer2[0])
        {
            Debug.WriteLine("Reading from disk 1...");
            bool result = ahciController.Value.AhciBaseMemoryPtr->Ports[1].Read(0x0, 0x0, 1, buffer2Ptr);
            Debug.WriteLine("Read complete.");
            GraphicsOutputProtocol.Print("Result:");
            GraphicsOutputProtocol.PrintLine(result.ToString());
        }
        GraphicsOutputProtocol.Print("Buffer:");
        for (int i = 0; i < buffer2.Length; i++)
        {
            GraphicsOutputProtocol.Print(((nuint)buffer2[i]).ToString(true));
            GraphicsOutputProtocol.Print(", ");
        }


        while (true) { }
    }

    public static void PrintEfiStatusCode(EFI_STATUS statusCode)
    {
        string resultCodeString = ((nuint)statusCode).ToString(true);
        Console.WriteLine(resultCodeString);
        object.Free(resultCodeString);
    }
}