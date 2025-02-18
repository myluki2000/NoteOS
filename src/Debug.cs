namespace NoteOS;

public static class Debug {
    public static void Write(char c) {
        IoPorts.outb(0xE9, (byte)c);
    }
    
    public static void Write(string message) {
        for(int i = 0; i < message.Length; i++) {
            Write(message[i]);
        }
    }

    public static void WriteLine(string message) {
        Write(message);
        Write('\n');
    }
}