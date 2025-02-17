using System;
using System.Threading;
using Internal.Runtime.CompilerHelpers;
using System.Runtime.InteropServices;
using NoteOS.ExtensionMethods;

namespace NoteOS.EfiProtocols;

public static unsafe class GraphicsOutputProtocol
{
    public static EFI_GRAPHICS_OUTPUT_PROTOCOL* gop;

    public static int CursorPositionX = 0;
    public static int CursorPositionY = 0;

    public static void Init()
    {
        gop = (EFI_GRAPHICS_OUTPUT_PROTOCOL*)EfiProtocol.Get(EfiProtocol.GUID_GOP_PROTOCOL);
    }

    public static void SetMode()
    {
        // get current mode
        nuint sizeOfInfo;
        EFI_GRAPHICS_OUTPUT_MODE_INFORMATION* info;
        EFI_STATUS gopQueryModeReturnCode = gop->QueryMode(gop,
                                                           gop->Mode == null ? 0 : gop->Mode->Mode,
                                                           &sizeOfInfo,
                                                           &info);

        if (gopQueryModeReturnCode != EFI_STATUS.EFI_SUCCESS)
        {
            Console.Write("Failed to query GOP mode! Return code:");
            Program.PrintEfiStatusCode(gopQueryModeReturnCode);
            while (true) { }
            // TODO: If status code is EFI_NOT_STARTED, then the GOP is not started and we need to start it
            // see here: https://wiki.osdev.org/GOP#Get_the_Current_Mode
        }

        uint numModes = gop->Mode->MaxMode;
        uint nativeMode = gop->Mode->Mode;

        // query available modes
        for (uint i = 0; i < numModes; i++)
        {
            EFI_STATUS queryModeReturnCode = gop->QueryMode(gop, i, &sizeOfInfo, &info);
            if (queryModeReturnCode != EFI_STATUS.EFI_SUCCESS)
            {
                Console.Write("Failed to query GOP mode! Return code:");
                Program.PrintEfiStatusCode(queryModeReturnCode);
                while (true) { }
            }

            Console.Write("Mode ");
            Console.Write((int)i);
            Console.Write(": ");
            if (i == nativeMode)
                Console.Write("(native) ");
            Console.Write((int)info->HorizontalResolution);
            Console.Write("x");
            Console.Write((int)info->VerticalResolution);
            Console.Write(" PixelsPerScanLine: ");
            Console.Write((int)info->PixelsPerScanLine);
            Console.Write(" PixelFormat: ");
            Console.WriteLine((int)info->PixelFormat);
        }

        // set video mode
        EFI_STATUS setModeReturnCode = gop->SetMode(gop, nativeMode);
        if (setModeReturnCode != EFI_STATUS.EFI_SUCCESS)
        {
            Console.Write("Failed to set GOP mode! Return code:");
            Program.PrintEfiStatusCode(setModeReturnCode);
            while (true) { }
        }
    }

    public static void PlotPixel32bpp(int x, int y, uint pixel)
    {
        uint* pixelAddress = (uint*)((long)gop->Mode->FrameBufferBase + 4 * gop->Mode->Info->PixelsPerScanLine * y + 4 * x);
        *pixelAddress = pixel;
    }

    public static void Print(char c) {
        if(c == '\n') {
            CursorPositionX = 0;
            CursorPositionY += 12;
            return;
        }

        PutChar(c, CursorPositionX, CursorPositionY);
        CursorPositionX += 7;
        if (CursorPositionX >= gop->Mode->Info->HorizontalResolution - 7) {
            CursorPositionX = 0;
            CursorPositionY += 12;
        }
    }

    public static void Print(string s) {
        for (int i = 0; i < s.Length; i++) {
            Print(s[i]);
        }
    }

    public static void Print(char* arr) {
        while(*arr != '\0') {
            Print(*arr);
            arr++;
        }
    }

    public static void PrintLine(string s) {
        Print(s);
        Print('\n');
    }

    public static void PutChar(char c, int x, int y) {
        int ix = 0;
        int iy = 0;

        for (byte i = 0; i < 12; i++) {
            byte curr_byte = charmap_byte((byte)c, i);

            for (int j = 0; j < 8; j++) {
                if ((curr_byte & (1 << j)) != 0) {
                    GraphicsOutputProtocol.PlotPixel32bpp(x + ix, y + iy, 0x00FFFFFF);
                }
                ix++;
                if(ix >= 7) {
                    ix = 0;
                    iy++;
                    break;
                }
            }
        }
    }

    public static void DrawRectangle(int x, int y, int width, int height, uint color)
    {
        for (int iy = y; iy < y + height; iy++)
        {
            for (int ix = x; ix < x + width; ix++)
            {
                GraphicsOutputProtocol.PlotPixel32bpp(ix, iy, color);
            }
        }
    }

    [DllImport("baselib", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    private static extern byte charmap_byte(byte c, byte i);
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_GRAPHICS_OUTPUT_PROTOCOL
{
    public readonly delegate* unmanaged<EFI_GRAPHICS_OUTPUT_PROTOCOL*, uint, nuint*, EFI_GRAPHICS_OUTPUT_MODE_INFORMATION**, EFI_STATUS> QueryMode;
    public readonly delegate* unmanaged<EFI_GRAPHICS_OUTPUT_PROTOCOL*, uint, EFI_STATUS> SetMode;
    public readonly delegate* unmanaged<EFI_GRAPHICS_OUTPUT_PROTOCOL*, EFI_GRAPHICS_OUTPUT_BLT_PIXEL*, EFI_GRAPHICS_OUTPUT_BLT_OPERATION, nuint, nuint, nuint, nuint, nuint, nuint, nuint, EFI_STATUS> Blt;
    public readonly EFI_GRAPHICS_OUTPUT_PROTOCOL_MODE* Mode;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_GRAPHICS_OUTPUT_BLT_PIXEL
{
    public readonly byte Blue;
    public readonly byte Green;
    public readonly byte Red;
    public readonly byte Reserved;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_GRAPHICS_OUTPUT_PROTOCOL_MODE
{
    public readonly uint MaxMode;
    public readonly uint Mode;
    public readonly EFI_GRAPHICS_OUTPUT_MODE_INFORMATION* Info;
    public readonly nuint SizeOfInfo;
    public readonly nuint FrameBufferBase; // is this type correct?
    public readonly nuint FrameBufferSize;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_GRAPHICS_OUTPUT_MODE_INFORMATION
{
    public readonly uint Version;
    public readonly uint HorizontalResolution;
    public readonly uint VerticalResolution;
    public readonly EFI_GRAPHICS_PIXEL_FORMAT PixelFormat;
    public readonly EFI_PIXEL_BITMASK PixelInformation;
    public readonly uint PixelsPerScanLine;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_PIXEL_BITMASK
{
    public readonly uint RedMask;
    public readonly uint GreenMask;
    public readonly uint BlueMask;
    public readonly uint ReservedMask;
}

public enum EFI_GRAPHICS_PIXEL_FORMAT : uint
{
    PixelRedGreenBlueReserved8BitPerColor,
    PixelBlueGreenRedReserved8BitPerColor,
    PixelBitMask,
    PixelBltOnly,
    PixelFormatMax
}

public enum EFI_GRAPHICS_OUTPUT_BLT_OPERATION : uint
{
    EfiBltVideoFill,
    EfiBltVideoToBltBuffer,
    EfiBltBufferToVideo,
    EfiBltVideoToVideo,
    EfiGRaphicsOutputBltOperationMax
}