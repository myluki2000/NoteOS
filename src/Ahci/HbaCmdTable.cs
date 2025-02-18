using System.Runtime.InteropServices;

namespace NoteOS.Ahci;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public unsafe struct HbaCmdTable
{
    public fixed byte CommandFis[64];
    public fixed byte AtapiCommand[16];
    public fixed byte Reserved[48];
    /// <summary>
    /// Physical region descriptor table entries, 0 ~ 65535
    /// </summary>
    public HbaPrdtEntry PrdtEntry;
}