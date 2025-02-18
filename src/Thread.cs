using System.Runtime.InteropServices;

namespace NoteOS;

public class Thread {
    public static void Sleep(int milliseconds) {
        // TODO: Implement sleep
    }

    public static void MemoryBarrier() {
        mfence();
    }

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    private static extern void mfence();
}