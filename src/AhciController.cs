using NoteOS.ExtensionMethods;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NoteOS;

public unsafe struct AhciController  {
    private Pci.PciDevice pciDevice;

    public HbaMemory* AhciBaseMemoryPtr = null;

    public AhciController(Pci.PciDevice pciDevice) {
        this.pciDevice = pciDevice;

        AhciBaseMemoryPtr = (HbaMemory*)pciDevice.ReadQword(0, 0x24);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct HbaMemory {
        public uint HostCapability;
        public uint GlobalHostControl;
        public uint InterruptStatus;
        public uint PortImplemented;
        public uint Version;
        public uint CommandCompletionCoalescingControl;
        public uint CommandCompletionCoalescingPorts;
        public uint EnclosureManagementLocation;
        public uint EnclosureManagementControl;
        public uint HostCapability2;
        public uint BiosHandoffControlStatus;
        public fixed byte Reserved[0xA0 - 0x2C];
        public fixed byte VendorSpecific[0x100 - 0xA0];
        public HbaPortInlineArray Ports;
    }

    [InlineArray(32)]
    public struct HbaPortInlineArray {
        HbaPort Element;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct HbaPort {
        public nuint CommandListBaseAddress;
        public nuint FisBaseAddress;
        public uint InterruptStatus;
        public uint InterruptEnable;
        public uint CommandAndStatus;
        public uint Reserved0;
        public uint TaskFileData;
        public uint Signature;
        public uint SataStatus;
        public uint SataControl;
        public uint SataError;
        public uint SataActive;
        public uint CommandIssue;
        public uint SataNotification;
        public uint FisSwitchControl;
        public fixed uint Reserved1[11];
        public fixed uint VendorSpecific[4];

        public bool HasDevice => (SataStatus & 0x0F) == 0x3;

        private const uint HBA_PxCMD_CR = 0x8000;
        private const uint HBA_PxCMD_FRE = 0x0010;
        private const uint HBA_PxCMD_ST = 0x0001;
        private const uint HBA_PxCMD_FR = 0x4000;
        private const byte ATA_CMD_WRITE_DMA_EXT = 0x25;
        private const uint HBA_PxIS_TFES = (1 << 30);


        /// <summary>
        /// Read "count" sectors from sector offset "starth:startl" to "buf" with LBA48 mode.
        /// </summary>
        public bool Read(nuint startl, uint starth, uint count, ushort* buf) {
            this.InterruptStatus = 0xFFFFFFFF;

            int slot = this.FindCmdSlot();

            //Console.Write("Command Slot: ");
            //Console.WriteLine(slot.ToString(true));

            if (slot == -1)
                return false;

            HbaCmdHeader* cmdHeader = (HbaCmdHeader*)(this.CommandListBaseAddress);
            cmdHeader += slot;

            cmdHeader->Byte0 = 0;
            // fis size is set in DWORDS, so divide by 4
            cmdHeader->Byte0 = (byte)((byte)(sizeof(FisRegH2d) / 4) & 0b11111); // command FIS size
            // 8k bytes (16 sectors) per PRDT, calculate how many entries we need based on how many (count) sectors we want to read
            cmdHeader->PhysicalRegionDescriptorTableLength = (short)(((count - 1) >> 4) + 1);

            HbaCmdTable* cmdTable = (HbaCmdTable*)(cmdHeader->CommandTableDescriptorBaseAddress);
            Unsafe.Memset(cmdTable, 0, (uint)(sizeof(HbaCmdTable) + (cmdHeader->PhysicalRegionDescriptorTableLength - 1) * sizeof(HbaPrdtEntry)));

            HbaPrdtEntry* entry;
            // 8k bytes (16 sectors) per PRDT
            for (int i = 0; i < cmdHeader->PhysicalRegionDescriptorTableLength - 1; i++) {
                entry = &(&cmdTable->PrdtEntry)[i];

                entry->DataBaseAddress = (uint)buf;

                entry->DoubleWord3 = 0;
                // set byte count to 8k - 1 (this value should always be set to 1 less than the actual value)
                entry->DoubleWord3 = 8 * 1024 - 1; // 8k bytes (16 sectors)
                // set the interrupt on completion bit to 1
                entry->DoubleWord3 |= (uint)1 << 31;

                buf += 4 * 1024; // 4k words
                count -= 16; // 16 sectors
            }
            // last entry
            entry = &(&cmdTable->PrdtEntry)[cmdHeader->PhysicalRegionDescriptorTableLength - 1];
            entry->DataBaseAddress = (uint)buf;

            entry->DoubleWord3 = 0;
            // remaining bytes
            entry->DoubleWord3 = (count % 8192) - 1;
            // set the interrupt on completion bit to 1
            entry->DoubleWord3 |= (uint)1 << 31;

            // Setup command
            FisRegH2d* cmdfis = (FisRegH2d*)(cmdTable->CommandFis);

            cmdfis->FisType = (byte)FisType.FIS_TYPE_REG_H2D;
            cmdfis->Byte1 = 0;
            cmdfis->Byte1 |= (1 << 7);
            cmdfis->Command = ATA_CMD_WRITE_DMA_EXT;

            cmdfis->LbaLow = (byte)startl;
            cmdfis->LbaMid = (byte)(startl >> 8);
            cmdfis->LbaHigh = (byte)(startl >> 16);
            cmdfis->Device = 1 << 6; // LBA mode

            cmdfis->LbaLowExp = (byte)(startl >> 24);
            cmdfis->LbaMidExp = (byte)starth;
            cmdfis->LbaHighExp = (byte)(starth >> 8);

            cmdfis->Count = (ushort)count;

            // spin lock timeout counter
            int spin = 0;

            // wait until the port is no longer busy before issuing a new command
            while((this.TaskFileData & (0x80 | 0x08)) != 0 && spin < 1000000) {
                spin++;
            }

            if(spin == 1000000) {
                //Console.WriteLine("Port is hung");
                return false;
            }

            this.CommandIssue = (uint)1 << slot;

            //Console.WriteLine("Waiting for command completion");

            // wait for completion
            while(true) {
                // In some longer duration reads, it may be helpful to spin on the DPS bit 
		        // in the PxIS port field as well (1 << 5)
                if((this.CommandIssue & (1 << slot)) == 0)
                    break;

                if((this.InterruptStatus & HBA_PxIS_TFES) != 0) {
                    // Task file error
                    //Console.WriteLine("Read disk error");
                    return false;
                }
            }

            //Console.WriteLine("Command completed");

            // check again
            if((this.InterruptStatus & HBA_PxIS_TFES) != 0) {
                // Task file error
                //Console.WriteLine("Read disk error");
                return false;
            }    

            return true;        
        }

        public bool Write(uint startl, uint starth, uint count, ushort* buf) {
            this.InterruptStatus = 0xFFFFFFFF;

            int slot = this.FindCmdSlot();
            if (slot == -1)
                return false;

            HbaCmdHeader* cmdHeader = (HbaCmdHeader*)(this.CommandListBaseAddress);
            cmdHeader += slot;

            cmdHeader->Byte0 = 0;
            // fis size is set in DWORDS, so divide by 4
            cmdHeader->Byte0 = (byte)((byte)(sizeof(FisRegH2d) / 4) & 0b11111); // command FIS size
            cmdHeader->Byte0 |= 0b1000000; // set write bit to 1
            cmdHeader->Byte1 = 0b100; // set clear busy bit to 1

            // 8k bytes (16 sectors) per PRDT, calculate how many entries we need based on how many (count) sectors we want to write
            cmdHeader->PhysicalRegionDescriptorTableLength = (short)(((count - 1) >> 4) + 1);

            HbaCmdTable* cmdTable = (HbaCmdTable*)(cmdHeader->CommandTableDescriptorBaseAddress);
            Unsafe.Memset(cmdTable, 0, (uint)(sizeof(HbaCmdTable) + (cmdHeader->PhysicalRegionDescriptorTableLength - 1) * sizeof(HbaPrdtEntry)));

            HbaPrdtEntry* entry;
            // 8k bytes (16 sectors) per PRDT
            for (int i = 0; i < cmdHeader->PhysicalRegionDescriptorTableLength - 1; i++) {
                entry = &(&cmdTable->PrdtEntry)[i];

                entry->DataBaseAddress = (uint)buf;

                entry->DoubleWord3 = 0;
                // set byte count to 8k - 1 (this value should always be set to 1 less than the actual value)
                entry->DoubleWord3 = 8 * 1024 - 1; // 8k bytes (16 sectors)
                // set the interrupt on completion bit to 1
                entry->DoubleWord3 |= (uint)1 << 31;

                buf += 4 * 1024; // 4k words
                count -= 16; // 16 sectors
            }
            // last entry
            entry = &(&cmdTable->PrdtEntry)[cmdHeader->PhysicalRegionDescriptorTableLength - 1];
            entry->DataBaseAddress = (uint)buf;

            entry->DoubleWord3 = 0;
            // remaining bytes
            entry->DoubleWord3 = (count % 8192) - 1;

            // Setup command
            FisRegH2d* cmdfis = (FisRegH2d*)(cmdTable->CommandFis);

            cmdfis->FisType = (byte)FisType.FIS_TYPE_REG_H2D;
            cmdfis->Byte1 = 0;
            cmdfis->Byte1 |= (1 << 7);
            cmdfis->Command = ATA_CMD_WRITE_DMA_EXT;

            cmdfis->LbaLow = (byte)startl;
            cmdfis->LbaMid = (byte)(startl >> 8);
            cmdfis->LbaHigh = (byte)(startl >> 16);
            cmdfis->Device = 1 << 6; // LBA mode

            cmdfis->LbaLowExp = (byte)(startl >> 24);
            cmdfis->LbaMidExp = (byte)starth;
            cmdfis->LbaHighExp = (byte)(starth >> 8);

            cmdfis->Count = (ushort)count;

            // spin lock timeout counter
            int spin = 0;

            // wait until the port is no longer busy before issuing a new command
            while((this.TaskFileData & (0x80 | 0x08)) != 0 && spin < 1000000) {
                spin++;
            }

            if(spin == 1000000) {
                //Console.WriteLine("Port is hung");
                return false;
            }

            this.CommandIssue = (uint)1 << slot;

            //Console.WriteLine("Waiting for command completion");

            // wait for completion
            while(true) {
                // In some longer duration reads, it may be helpful to spin on the DPS bit 
		        // in the PxIS port field as well (1 << 5)
                if((this.CommandIssue & (1 << slot)) == 0)
                    break;

                if((this.InterruptStatus & HBA_PxIS_TFES) != 0) {
                    // Task file error
                    //Console.WriteLine("Read disk error");
                    return false;
                }
            }

            //Console.WriteLine("Command completed");

            // check again
            if((this.InterruptStatus & HBA_PxIS_TFES) != 0) {
                // Task file error
                //Console.WriteLine("Read disk error");
                return false;
            }    

            return true;
        }

        public int FindCmdSlot() {
            // If not set in SACT and CI, the slot is free
            uint slots = (this.CommandIssue | this.SataActive);

            // can support between 1 and 32 slots, TODO: we can get the number of slots by checking the host capability register
            const int cmdslots = 1;

            for(int i = 0; i < cmdslots; i++) {
                if((slots & 1) == 0)
                    return i;
                
                slots >>= 1;
            }

            return -1;
        }

        public void Rebase(nuint portBaseAddress) {
            this.StopCommandEngine();

            // 1024k bytes for the command list (32 entries * 32 bytes each)
            this.CommandListBaseAddress = portBaseAddress;
            Unsafe.Memset((void*)this.CommandListBaseAddress, 0, 1024);

            // 256 bytes for the FIS
            this.FisBaseAddress = portBaseAddress + 1024;
            Unsafe.Memset((void*)this.FisBaseAddress, 0, 256);

            HbaCmdHeader* cmdHeader = (HbaCmdHeader*)(this.CommandListBaseAddress);
            for(int i = 0; i < 32; i++) {
                cmdHeader[i].PhysicalRegionDescriptorTableLength = 8; // 8 prdt entries per command table
                cmdHeader[i].PhysicalRegionDescriptorByteCountTransferred = 0;
                cmdHeader[i].CommandTableDescriptorBaseAddress = portBaseAddress + (nuint)(1024 + 256 + (i * 256));
                Unsafe.Memset((void*)cmdHeader[i].CommandTableDescriptorBaseAddress, 0, 256);
            }

            this.StartCommandEngine();
        }

        public void StartCommandEngine() {
            while((this.CommandAndStatus & HBA_PxCMD_CR) != 0) {
                // wait until CR is cleared
            }

            // Set FRE (bit 4) and ST (bit 0)
            this.CommandAndStatus |= (HBA_PxCMD_FRE | HBA_PxCMD_ST);
        }

        public void StopCommandEngine() {
            // Clear ST (bit 0)
            this.CommandAndStatus &= ~HBA_PxCMD_ST;

            // Clear FRE (bit 4)
            this.CommandAndStatus &= ~HBA_PxCMD_FRE;

            // wait until FR (bit 14) and CR (bit 15) are cleared
            while((this.CommandAndStatus & (HBA_PxCMD_FR | HBA_PxCMD_CR)) != 0) {
                // wait until FR and CR are cleared
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct HbaCmdHeader {
        /// <summary>
        /// Bits 0-4: Command FIS length in DWORDS, 2 ~ 16
        /// Bit 5: ATAPI
        /// Bit 6: Write, 1: H2D, 0: D2H
        /// Bit 7: Prefetchable
        /// </summary>
        public byte Byte0;
        /// <summary>
        /// Bit 0: Reset
        /// Bit 1: BIST
        /// Bit 2: Clear busy upon R_OK
        /// Bit 3: Reserved
        /// Bit 4-7: Port multiplier port
        /// </summary>
        public byte Byte1;
        /// <summary>
        /// Physical Region Descriptor Table length in entries.
        /// </summary>
        public short PhysicalRegionDescriptorTableLength;
        public uint PhysicalRegionDescriptorByteCountTransferred;
        public nuint CommandTableDescriptorBaseAddress;
        public fixed uint Reserved[4];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct HbaCmdTable {
        public fixed byte CommandFis[64];
        public fixed byte AtapiCommand[16];
        public fixed byte Reserved[48];
        /// <summary>
        /// Physical region descriptor table entries, 0 ~ 65535
        /// </summary>
        public HbaPrdtEntry PrdtEntry;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct HbaPrdtEntry {
        public uint DataBaseAddress;
        public uint DataBaseAddressUpper;
        public uint Reserved0;

        /// <summary>
        /// Byte 0-21: Byte count, 4M max
        /// Byte 22-30: Reserved
        /// Bit 31: Interrupt on completion
        /// </summary>
        public uint DoubleWord3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct FisRegH2d {
        /// <summary>
        /// FIS_TYPE_REG_H2D
        /// </summary>
        public byte FisType;
        /// <summary>
        /// Bit 0-3: Port multiplier
        /// Bit 4-6: Reserved
        /// Bit 7: Command, 1: Command, 0: Control
        /// </summary>
        public byte Byte1;
        /// <summary>
        /// Command register
        /// </summary>
        public byte Command;
        /// <summary>
        /// Features register, 7:0
        /// </summary>
        public byte FeatureLow;

        public byte LbaLow;
        public byte LbaMid;
        public byte LbaHigh;
        public byte Device;

        public byte LbaLowExp;
        public byte LbaMidExp;
        public byte LbaHighExp;
        public byte FeatureHigh;

        public ushort Count;

        public byte IsochronousCommandCompletion;
        public byte Control;

        public fixed byte Reserved[4];
    }

    public enum FisType {
        /// <summary>
        /// Register FIS - host to device
        /// </summary>
        FIS_TYPE_REG_H2D	= 0x27,
        /// <summary>
        /// Register FIS - device to host
        /// </summary>
	    FIS_TYPE_REG_D2H	= 0x34,
        /// <summary>
        /// DMA activate FIS - device to host
        /// </summary>
	    FIS_TYPE_DMA_ACT	= 0x39,
        /// <summary>
        /// DMA setup FIS - bidirectional
        /// </summary>
	    FIS_TYPE_DMA_SETUP	= 0x41,
        /// <summary>
        /// Data FIS - bidirectional
        /// </summary>
	    FIS_TYPE_DATA		= 0x46,
        /// <summary>
        /// BIST activate FIS - bidirectional
        /// </summary>
	    FIS_TYPE_BIST		= 0x58,
        /// <summary>
        /// PIO setup FIS - device to host
        /// </summary>
	    FIS_TYPE_PIO_SETUP	= 0x5F,
        /// <summary>
        /// Set device bits FIS - device to host
        /// </summary>
	    FIS_TYPE_DEV_BITS	= 0xA1,
    }
}