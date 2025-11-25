// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).
#pragma warning disable CA1720 // Legacy LuaPort identifiers intentionally match upstream pointer naming.

    using System;
    using lua_Integer = System.Int32;

    public static partial class LuaBase
    {
        internal static lua_Integer MemoryCompare(CharPtr ptr1, CharPtr ptr2, uint size)
        {
            return MemoryCompare(ptr1, ptr2, (int)size);
        }

        internal static int MemoryCompare(CharPtr ptr1, CharPtr ptr2, int size)
        {
            EnsurePointer(ptr1, nameof(ptr1));
            EnsurePointer(ptr2, nameof(ptr2));
            for (int i = 0; i < size; i++)
            {
                if (ptr1[i] != ptr2[i])
                {
                    if (ptr1[i] < ptr2[i])
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }

        internal static CharPtr MemoryFindCharacter(CharPtr ptr, char c, uint count)
        {
            EnsurePointer(ptr, nameof(ptr));
            for (uint i = 0; i < count; i++)
            {
                if (ptr[i] == c)
                {
                    return new CharPtr(ptr.chars, (int)(ptr.index + i));
                }
            }

            return null;
        }

        internal static CharPtr StringFindAny(CharPtr str, CharPtr charset)
        {
            EnsurePointer(str, nameof(str));
            EnsurePointer(charset, nameof(charset));
            for (int i = 0; str[i] != '\0'; i++)
            {
                for (int j = 0; charset[j] != '\0'; j++)
                {
                    if (str[i] == charset[j])
                    {
                        return new CharPtr(str.chars, str.index + i);
                    }
                }
            }

            return null;
        }

        internal static bool IsAlpha(char c)
        {
            return Char.IsLetter(c);
        }

        internal static bool IsControl(char c)
        {
            return Char.IsControl(c);
        }

        internal static bool IsDigit(char c)
        {
            return Char.IsDigit(c);
        }

        internal static bool IsLower(char c)
        {
            return Char.IsLower(c);
        }

        internal static bool IsPunctuation(char c)
        {
            return Char.IsPunctuation(c);
        }

        internal static bool IsSpace(char c)
        {
            return (c == ' ') || (c >= (char)0x09 && c <= (char)0x0D);
        }

        internal static bool IsUpper(char c)
        {
            return Char.IsUpper(c);
        }

        internal static bool IsAlphanumeric(char c)
        {
            return Char.IsLetterOrDigit(c);
        }

        internal static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        internal static bool IsGraphical(char c)
        {
            return !Char.IsControl(c) && !Char.IsWhiteSpace(c);
        }

        internal static bool IsAlpha(int c)
        {
            return Char.IsLetter((char)c);
        }

        internal static bool IsControl(int c)
        {
            return Char.IsControl((char)c);
        }

        internal static bool IsDigit(int c)
        {
            return Char.IsDigit((char)c);
        }

        internal static bool IsLower(int c)
        {
            return Char.IsLower((char)c);
        }

        internal static bool IsPunctuation(int c)
        {
            return ((char)c != ' ') && !IsAlphanumeric((char)c);
        } // *not* the same as Char.IsPunctuation

        internal static bool IsSpace(int c)
        {
            return ((char)c == ' ') || ((char)c >= (char)0x09 && (char)c <= (char)0x0D);
        }

        internal static bool IsUpper(int c)
        {
            return Char.IsUpper((char)c);
        }

        internal static bool IsAlphanumeric(int c)
        {
            return Char.IsLetterOrDigit((char)c);
        }

        internal static bool IsGraphical(int c)
        {
            return !Char.IsControl((char)c) && !Char.IsWhiteSpace((char)c);
        }

        internal static char ToLower(char c)
        {
            return Char.ToLowerInvariant(c);
        }

        internal static char ToUpper(char c)
        {
            return Char.ToUpperInvariant(c);
        }

        internal static char ToLower(int c)
        {
            return Char.ToLowerInvariant((char)c);
        }

        internal static char ToUpper(int c)
        {
            return Char.ToUpperInvariant((char)c);
        }

        // find c in str
        internal static CharPtr StringFindCharacter(CharPtr str, char c)
        {
            EnsurePointer(str, nameof(str));
            for (int index = str.index; str.chars[index] != 0; index++)
            {
                if (str.chars[index] == c)
                {
                    return new CharPtr(str.chars, index);
                }
            }

            return null;
        }

        internal static CharPtr StringCopy(CharPtr dst, CharPtr src)
        {
            EnsurePointer(dst, nameof(dst));
            EnsurePointer(src, nameof(src));
            int i;
            for (i = 0; src[i] != '\0'; i++)
            {
                dst[i] = src[i];
            }

            dst[i] = '\0';
            return dst;
        }

        internal static CharPtr StringCopyWithLength(CharPtr dst, CharPtr src, int length)
        {
            EnsurePointer(dst, nameof(dst));
            EnsurePointer(src, nameof(src));
            int index = 0;
            while ((src[index] != '\0') && (index < length))
            {
                dst[index] = src[index];
                index++;
            }
            while (index < length)
            {
                dst[index++] = '\0';
            }

            return dst;
        }

        internal static int StringLength(CharPtr str)
        {
            EnsurePointer(str, nameof(str));
            int index = 0;
            while (str[index] != '\0')
            {
                index++;
            }

            return index;
        }

        public static void StringFormat(CharPtr buffer, CharPtr str, params object[] argv)
        {
            EnsurePointer(buffer, nameof(buffer));
            EnsurePointer(str, nameof(str));
            string temp = Tools.StringFormat(str.ToString(), argv);
            StringCopy(buffer, temp);
        }
    }
}
