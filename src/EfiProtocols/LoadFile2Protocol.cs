namespace NoteOS.EfiProtocols;

public static unsafe class LoadFile2Protocol {
    public static EFI_LOAD_FILE_PROTOCOL* loadFile2;

    public static void Init() {
        loadFile2 = (EFI_LOAD_FILE_PROTOCOL*)EfiProtocol.Get(EfiProtocol.GUID_LOAD_FILE2_PROTOCOL);
    }
}



