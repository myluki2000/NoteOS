using NoteOS;
using System.Collections.Generic;

namespace NoteOS;

public static class Pci {
    private static ushort ReadConfigWord(byte bus, byte slot, byte func, byte offset) {
        uint address;
        uint lbus = bus;
        uint lslot = slot;
        uint lfunc = func;
        ushort tmp = 0;

        // create configuration address
        address = (uint)((lbus << 16) | (lslot << 11) |
              (lfunc << 8) | (offset & 0xFC) | ((uint)0x80000000));

        // write out the address
        IoPorts.outl(0xCF8, address);
        // read in the data
        // (offset & 2) * 8) = 0 will choose the first word of the 32-bit register
        tmp = (ushort)((IoPorts.inl(0xCFC) >> ((offset & 2) * 8)) & 0xFFFF);
        return tmp;
    }

    public static bool SlotHasDevice(byte bus, byte slot) {
        // If vendor is 0xFFFF, there is no device
        return new PciDevice(bus, slot).VendorId != 0xFFFF;
    }

    public struct PciDevice {
        public PciDevice(byte bus, byte slot) {
            Bus = bus;
            Slot = slot;
        }

        public byte Bus { get; }
        public byte Slot { get; }

        public ushort VendorId => ReadConfigWord(Bus, Slot, 0, 0);

        public ushort DeviceId => ReadConfigWord(Bus, Slot, 0, 2);

        public byte ClassId => (byte)(ReadConfigWord(Bus, Slot, 0, 0x0A) >> 8);

        public byte SubclassId => (byte)(ReadConfigWord(Bus, Slot, 0, 0x0A) & 0xFF);

        public byte ProgrammingInterface => (byte)(ReadConfigWord(Bus, Slot, 0, 0x09) >> 8);

        public ushort Status => ReadConfigWord(Bus, Slot, 0, 4);

        public ushort ReadWord(byte func, byte offset) {
            return ReadConfigWord(Bus, Slot, func, offset);
        }

        public byte ReadByte(byte func, byte offset) {
            return (byte)(ReadConfigWord(Bus, Slot, func, offset) & 0xFF);
        }

        public uint ReadDword(byte func, byte offset) {
            return ReadConfigWord(Bus, Slot, func, offset) | (uint)ReadConfigWord(Bus, Slot, func, (byte)(offset + 2)) << 16;
        }

        public ulong ReadQword(byte func, byte offset) {
            return ReadConfigWord(Bus, Slot, func, offset) | (ulong)ReadConfigWord(Bus, Slot, func, (byte)(offset + 2)) << 16 | (ulong)ReadConfigWord(Bus, Slot, func, (byte)(offset + 4)) << 32 | (ulong)ReadConfigWord(Bus, Slot, func, (byte)(offset + 6)) << 48;
        }
    }

    public static unsafe int GetDevices(PciDevice* devices) {
        int deviceCount = 0;
        for(ushort bus = 0; bus < 256; bus++) {
            for(ushort slot = 0; slot < 32; slot++) {
                if(SlotHasDevice((byte)bus, (byte)slot)) {
                    devices[deviceCount] = new PciDevice((byte)bus, (byte)slot);
                    deviceCount++;
                }
            }
        }

        return deviceCount;
    }
}