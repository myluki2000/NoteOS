using NoteOS.ExtensionMethods;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NoteOS;

namespace NoteOS.Ahci;

public unsafe struct AhciController  {
    private Pci.PciDevice pciDevice;

    public HbaMemory* AhciBaseMemoryPtr = null;

    public AhciController(Pci.PciDevice pciDevice) {
        this.pciDevice = pciDevice;

        pciDevice.MemorySpaceEnabled = true;
        pciDevice.BusMasterEnabled = true;

        AhciBaseMemoryPtr = (HbaMemory*)pciDevice.ReadQWord(0, 0x24);
    }
}