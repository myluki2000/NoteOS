using System.Runtime.InteropServices;

namespace NoteOS;

public static unsafe class IoPorts {
    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    public static extern byte inb(ushort port);

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    public static extern void outb(ushort port, byte val);

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    public static extern ushort inw(ushort port);

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    public static extern void outw(ushort port, ushort val);

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    public static extern uint inl(ushort port);

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    public static extern void outl(ushort port, uint val);
}