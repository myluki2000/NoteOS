using System;

struct Program {
    public static unsafe void Main() {
        while(true) {
            Console.WriteLine("Hello, World!");

            #if UEFI
                EfiSystemTable->BootServices->Stall(1000 * 1000);
            #endif
        }
    }
}