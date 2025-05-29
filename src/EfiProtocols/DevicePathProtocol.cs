using System.Runtime.InteropServices;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using NoteOS.ExtensionMethods;

namespace NoteOS.EfiProtocols;

public static unsafe class DevicePathProtocol {
    public static EFI_DEVICE_PATH_PROTOCOL* devicePath;
    public static EFI_DEVICE_PATH_UTILITIES_PROTOCOL* utilities;

    public static void Init() {
        devicePath = (EFI_DEVICE_PATH_PROTOCOL*)EfiProtocol.Get(EfiProtocol.GUID_DEVICE_PATH_PROTOCOL);
        utilities = (EFI_DEVICE_PATH_UTILITIES_PROTOCOL*)EfiProtocol.Get(EfiProtocol.GUID_DEVICE_PATH_UTILITIES_PROTOCOL);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe readonly struct EFI_DEVICE_PATH_PROTOCOL {
    public readonly byte Type;
    public readonly byte SubType;
    public readonly ushort Length;
    // after the length field, a data section of size (Length - 4) follows

    public ReadOnlySpan<byte> GetData() {
        fixed(ushort* lengthPtr = &Length) {
            byte* dataPtr = (byte*)lengthPtr + 2;
            return new ReadOnlySpan<byte>(dataPtr, Length - 4);
        }
    }

    public byte* GetDataPointer() {
        fixed(ushort* lengthPtr = &Length) {
            byte* dataPtr = (byte*)lengthPtr + 2;
            return dataPtr;
        }
    }

    public EFI_DEVICE_PATH_PROTOCOL* GetNextNode() {
        // if this is a path end node, there is no next node
        if (Type == 0x7F && SubType == 0xFF && Length == 4)
            return null;

        fixed(byte* thisPtr = &Type)
            return (EFI_DEVICE_PATH_PROTOCOL*)(thisPtr + Length);
    }

    public void Print() {
        Console.Write("Type: ");
        Console.Write((int)Type);
        Console.Write(" SubType: ");
        Console.Write((int)SubType);
        Console.Write(" Length: ");
        Console.Write((int)Length);
        
        if(Length - 4 > 0) {
            Console.Write(" Data: ");
            
            ReadOnlySpan<byte> data = GetData();
            if(Type == 4 && SubType == 4) {
                // print file path as string
                char* str = (char*)GetDataPointer();
                Console.Write(str);
            } else {
                for(int i = 0; i < data.Length; i++) {
                    byte b = data[i];
                    string hex = ((nuint)b).ToString(true);
                    Console.Write(hex);
                    object.Free(hex);
                    Console.Write(" ");
                }
            }
        }

        Console.WriteLine();
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EFI_DEVICE_PATH_UTILITIES_PROTOCOL {
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL*, nuint> GetDevicePathSize;
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*> DuplicateDevicePath;
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*> AppendDevicePath;
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*> AppendDeviceNode;
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*, EFI_DEVICE_PATH_PROTOCOL*> AppendDevicePathInstance;
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL**, nuint*, EFI_DEVICE_PATH_PROTOCOL*> GetNextDevicePathInstance;
    public readonly delegate* unmanaged<EFI_DEVICE_PATH_PROTOCOL*, bool> IsDevicePathMultiInstance;
    public readonly delegate* unmanaged<byte, byte, ushort, EFI_DEVICE_PATH_PROTOCOL*> CreateDeviceNode;
}