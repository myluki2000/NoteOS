using NoteOS;
using System.Collections.Generic;

namespace NoteOS;

public static class Pci {
    private static uint ReadConfigDWord(byte bus, byte slot, byte func, byte offset) {
        uint address;
        uint lbus = bus;
        uint lslot = slot;
        uint lfunc = func;

        // create configuration address
        address = (uint)((lbus << 16) | (lslot << 11) |
              (lfunc << 8) | offset | ((uint)0x80000000));

        // write out the address
        IoPorts.outl(0xCF8, address);
        // read in the data
        return IoPorts.inl(0xCFC);
    }

    private static void WriteConfigDWord(byte bus, byte slot, byte func, byte offset, uint value) {
        uint address;
        uint lbus = bus;
        uint lslot = slot;
        uint lfunc = func;

        // create configuration address
        address = (uint)((lbus << 16) | (lslot << 11) |
              (lfunc << 8) | offset | ((uint)0x80000000));

        // write out the address
        IoPorts.outl(0xCF8, address);
        // write out the data
        IoPorts.outl(0xCFC, value);
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

        public ushort VendorId => (ushort)ReadConfigDWord(Bus, Slot, 0, 0);

        public ushort DeviceId => (ushort)(ReadConfigDWord(Bus, Slot, 0, 0) >> 16);

        public byte ClassId => (byte)(ReadConfigDWord(Bus, Slot, 0, 0x8) >> 24);

        public byte SubclassId => (byte)(ReadConfigDWord(Bus, Slot, 0, 0x8) >> 16);

        public byte ProgrammingInterface => (byte)(ReadConfigDWord(Bus, Slot, 0, 0x8) >> 8);

        public ushort Status => (ushort)(ReadConfigDWord(Bus, Slot, 0, 0x4) >> 16);

        public bool MemorySpaceEnabled {
            get => (ReadConfigDWord(Bus, Slot, 0, 0x4) & MEMORY_SPACE_ENABLED_BITMASK) != 0;
            set {
                uint configValue = (ushort)ReadConfigDWord(Bus, Slot, 0, 0x4);

                if(value) {
                    configValue |= MEMORY_SPACE_ENABLED_BITMASK;
                } else {
                    configValue &= ~MEMORY_SPACE_ENABLED_BITMASK;
                }
            }
        }

        public bool IoSpaceEnabled {
            get => (ReadConfigDWord(Bus, Slot, 0, 0x4) & IO_SPACE_ENABLED_BITMASK) != 0;
            set {
                uint configValue = (ushort)ReadConfigDWord(Bus, Slot, 0, 0x4);

                if(value) {
                    configValue |= IO_SPACE_ENABLED_BITMASK;
                } else {
                    configValue &= ~IO_SPACE_ENABLED_BITMASK;
                }
            }
        }

        public bool BusMasterEnabled {
            get => (ReadConfigDWord(Bus, Slot, 0, 0x4) & BUS_MASTER_ENABLED_BITMASK) != 0;
            set {
                uint configValue = (ushort)ReadConfigDWord(Bus, Slot, 0, 0x4);

                if(value) {
                    configValue |= BUS_MASTER_ENABLED_BITMASK;
                } else {
                    configValue &= ~BUS_MASTER_ENABLED_BITMASK;
                }
            }
        }

        public bool ParityErrorResponse {
            get => (ReadConfigDWord(Bus, Slot, 0, 0x4) & PARITY_ERROR_RESPONSE_BITMASK) != 0;
            set {
                uint configValue = (ushort)ReadConfigDWord(Bus, Slot, 0, 0x4);

                if(value) {
                    configValue |= PARITY_ERROR_RESPONSE_BITMASK;
                } else {
                    configValue &= ~PARITY_ERROR_RESPONSE_BITMASK;
                }
            }
        }

        public bool SerrEnabled {
            get => (ReadConfigDWord(Bus, Slot, 0, 0x4) & SERR_ENABLED_BITMASK) != 0;
            set {
                uint configValue = (ushort)ReadConfigDWord(Bus, Slot, 0, 0x4);

                if(value) {
                    configValue |= SERR_ENABLED_BITMASK;
                } else {
                    configValue &= ~SERR_ENABLED_BITMASK;
                }
            }
        }

        public bool InterruptDisabled {
            get => (ReadConfigDWord(Bus, Slot, 0, 0x4) & INTERRUPT_DISABLED_BITMASK) != 0;
            set {
                uint configValue = (ushort)ReadConfigDWord(Bus, Slot, 0, 0x4);

                if(value) {
                    configValue |= INTERRUPT_DISABLED_BITMASK;
                } else {
                    configValue &= ~INTERRUPT_DISABLED_BITMASK;
                }
            }
        }

        private const uint MEMORY_SPACE_ENABLED_BITMASK = 0b10;
        private const uint IO_SPACE_ENABLED_BITMASK = 0b1;
        private const uint BUS_MASTER_ENABLED_BITMASK = 0b100;
        private const uint PARITY_ERROR_RESPONSE_BITMASK = 0b1000000;
        private const uint SERR_ENABLED_BITMASK = 0b100000000;
        private const uint INTERRUPT_DISABLED_BITMASK = 0b10000000000;

        public uint ReadDWord(byte func, byte offset) {
            return ReadConfigDWord(Bus, Slot, func, offset);
        }

        public ulong ReadQWord(byte func, byte offset) {
            return (ulong)ReadConfigDWord(Bus, Slot, func, offset) | ((ulong)ReadConfigDWord(Bus, Slot, func, (byte)(offset + 4)) << 32);
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