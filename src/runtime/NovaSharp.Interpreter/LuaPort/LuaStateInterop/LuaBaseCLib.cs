// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).
#pragma warning disable CA1720 // Legacy LuaPort identifiers intentionally match upstream pointer naming.

    using System;
    using lua_Integer = System.Int32;

    public partial class LuaBase
    {
        protected static lua_Integer Memcmp(CharPtr ptr1, CharPtr ptr2, uint size)
        {
            return Memcmp(ptr1, ptr2, (int)size);
        }

        protected static int Memcmp(CharPtr ptr1, CharPtr ptr2, int size)
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

        protected static CharPtr Memchr(CharPtr ptr, char c, uint count)
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

        protected static CharPtr Strpbrk(CharPtr str, CharPtr charset)
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

        protected static bool Isalpha(char c)
        {
            return Char.IsLetter(c);
        }

        protected static bool Iscntrl(char c)
        {
            return Char.IsControl(c);
        }

        protected static bool Isdigit(char c)
        {
            return Char.IsDigit(c);
        }

        protected static bool Islower(char c)
        {
            return Char.IsLower(c);
        }

        protected static bool Ispunct(char c)
        {
            return Char.IsPunctuation(c);
        }

        protected static bool Isspace(char c)
        {
            return (c == ' ') || (c >= (char)0x09 && c <= (char)0x0D);
        }

        protected static bool Isupper(char c)
        {
            return Char.IsUpper(c);
        }

        protected static bool Isalnum(char c)
        {
            return Char.IsLetterOrDigit(c);
        }

        protected static bool Isxdigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        protected static bool Isgraph(char c)
        {
            return !Char.IsControl(c) && !Char.IsWhiteSpace(c);
        }

        protected static bool Isalpha(int c)
        {
            return Char.IsLetter((char)c);
        }

        protected static bool Iscntrl(int c)
        {
            return Char.IsControl((char)c);
        }

        protected static bool Isdigit(int c)
        {
            return Char.IsDigit((char)c);
        }

        protected static bool Islower(int c)
        {
            return Char.IsLower((char)c);
        }

        protected static bool Ispunct(int c)
        {
            return ((char)c != ' ') && !Isalnum((char)c);
        } // *not* the same as Char.IsPunctuation

        protected static bool Isspace(int c)
        {
            return ((char)c == ' ') || ((char)c >= (char)0x09 && (char)c <= (char)0x0D);
        }

        protected static bool Isupper(int c)
        {
            return Char.IsUpper((char)c);
        }

        protected static bool Isalnum(int c)
        {
            return Char.IsLetterOrDigit((char)c);
        }

        protected static bool Isgraph(int c)
        {
            return !Char.IsControl((char)c) && !Char.IsWhiteSpace((char)c);
        }

        protected static char Tolower(char c)
        {
            return Char.ToLowerInvariant(c);
        }

        protected static char Toupper(char c)
        {
            return Char.ToUpperInvariant(c);
        }

        protected static char Tolower(int c)
        {
            return Char.ToLowerInvariant((char)c);
        }

        protected static char Toupper(int c)
        {
            return Char.ToUpperInvariant((char)c);
        }

        // find c in str
        protected static CharPtr Strchr(CharPtr str, char c)
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

        protected static CharPtr Strcpy(CharPtr dst, CharPtr src)
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

        protected static CharPtr Strncpy(CharPtr dst, CharPtr src, int length)
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

        protected static int Strlen(CharPtr str)
        {
            EnsurePointer(str, nameof(str));
            int index = 0;
            while (str[index] != '\0')
            {
                index++;
            }

            return index;
        }

        public static void Sprintf(CharPtr buffer, CharPtr str, params object[] argv)
        {
            EnsurePointer(buffer, nameof(buffer));
            EnsurePointer(str, nameof(str));
            string temp = Tools.Sprintf(str.ToString(), argv);
            Strcpy(buffer, temp);
        }
    }
}
