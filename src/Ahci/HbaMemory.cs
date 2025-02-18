using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NoteOS.Ahci;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct HbaMemory
{
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
public struct HbaPortInlineArray
{
    HbaPort Element;
}