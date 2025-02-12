using System;
using System.Numerics;

namespace NoteOS.ExtensionMethods
{
    public static unsafe class IntExtensions
    {
        public static unsafe string ToString<T>(this T value) where T : struct, IBinaryInteger<T> {
            T temp = value;

            T ten = T.Create(10);

            // number of digits in value
            int digits = 0;
            do {
                digits++;
                temp /= ten;
            } while(temp != T.Zero);

            // one extra for null terminator
            char* buffer = stackalloc char[2 + digits + 1];

            for(ushort i = 0; i < digits; i++) {
                T digitChar = (value % ten) + T.Create(48);
                buffer[digits - i - 1] = *(char*)(&digitChar);
                value /= ten;
            }

            buffer[digits] = '\0';

            return new string(buffer);
        }

        public static string ToString<T>(this T value, bool hexFormat, char* arr = null) where T : struct, IBinaryInteger<T> {
            if(!hexFormat) {
                return value.ToString();
            } else {
                T temp = value;

                T sixteen = T.Create(16);

                // number of digits in value
                int digits = 0;
                do {
                    digits++;
                    temp /= sixteen;
                } while(temp != T.Zero);

                // one extra for null terminator, two extra for "0x" prefix
                char* buffer = stackalloc char[digits + 3];

                for(ushort i = 0; i < digits; i++) {
                    T digitChar = (value % sixteen);
                    byte digit = *(byte*)&digitChar;
                    if(digit < 10) {
                        buffer[2 + digits - i - 1] = (char)(digit + 48);
                    } else {
                        buffer[2 + digits - i - 1] = (char)(digit + 55);
                    }
                    value /= sixteen;
                }

                buffer[0] = '0';
                buffer[1] = 'x';
                buffer[2 + digits] = '\0';

                if(arr != null) {
                    for(int i = 0; i < digits + 3; i++) {
                        arr[i] = buffer[i];
                    }
                    return null;
                }

                return new string(buffer);
            }
        }
    }
}