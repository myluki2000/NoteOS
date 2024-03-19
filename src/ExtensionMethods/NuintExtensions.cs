namespace NoteOS.ExtensionMethods
{
    public static unsafe class IntExtensions
    {
        public static string ToString(this nuint value) {
            nuint temp = value;

            // number of digits in value
            int digits = 0;
            do {
                digits++;
                temp /= 10;
            } while(temp != 0);

            // one extra for null terminator
            char* buffer = stackalloc char[2 + digits + 1];

            for(ushort i = 0; i < digits; i++) {
                buffer[digits - i - 1] = (char)((value % 10) + 48);
                value /= 10;
            }

            buffer[digits] = '\0';

            return new string(buffer);
        }

        public static string ToString(this nuint value, bool hexFormat) {
            if(!hexFormat) {
                return value.ToString();
            } else {
                nuint temp = value;

                // number of digits in value
                int digits = 0;
                do {
                    digits++;
                    temp /= 16;
                } while(temp != 0);

                // one extra for null terminator, two extra for "0x" prefix
                char* buffer = stackalloc char[digits + 3];

                for(ushort i = 0; i < digits; i++) {
                    byte digit = (byte)(value % 16);
                    if(digit < 10) {
                        buffer[2 + digits - i - 1] = (char)(digit + 48);
                    } else {
                        buffer[2 + digits - i - 1] = (char)(digit + 55);
                    }
                    value /= 16;
                }

                buffer[0] = '0';
                buffer[1] = 'x';
                buffer[2 + digits] = '\0';

                return new string(buffer);
            }
        }
    }
}