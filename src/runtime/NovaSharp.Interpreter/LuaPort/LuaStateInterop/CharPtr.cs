namespace NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).
#pragma warning disable CA1720 // Legacy LuaPort identifiers intentionally match upstream pointer naming.
#pragma warning disable CA1725 // Legacy LuaPort overrides keep upstream parameter shapes for readability.

    //
    // This part taken from KopiLua - https://github.com/NLua/KopiLua
    //
    // =========================================================================================================
    //
    // Kopi Lua License
    // ----------------
    // MIT License for KopiLua
    // Copyright (c) 2012 LoDC
    // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
    // associated documentation files (the "Software"), to deal in the Software without restriction,
    // including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
    // and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
    // subject to the following conditions:
    // The above copyright notice and this permission notice shall be included in all copies or substantial
    // portions of the Software.
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
    // LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    // IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    // WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    // SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    // ===============================================================================
    // Lua License
    // -----------
    // Lua is licensed under the terms of the MIT license reproduced below.
    // This means that Lua is free software and can be used for both academic
    // and commercial purposes at absolutely no cost.
    // For details and rationale, see http://www.lua.org/license.html .
    // ===============================================================================
    // Copyright (C) 1994-2008 Lua.org, PUC-Rio.
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.

    using System;
    using System.Diagnostics;

    public class CharPtr
    {
        internal char[] chars;
        internal int index;

        public char this[int offset]
        {
            get { return chars[index + offset]; }
            set { chars[index + offset] = value; }
        }

        public char this[uint offset]
        {
            get { return chars[index + offset]; }
            set { chars[index + offset] = value; }
        }
        public char this[long offset]
        {
            get { return chars[index + (int)offset]; }
            set { chars[index + (int)offset] = value; }
        }

        public static implicit operator CharPtr(string str)
        {
            return new CharPtr(str);
        }

        public static implicit operator CharPtr(char[] chars)
        {
            return new CharPtr(chars);
        }

        public static implicit operator CharPtr(byte[] bytes)
        {
            return new CharPtr(bytes);
        }

        public static CharPtr FromString(string value)
        {
            return new CharPtr(value);
        }

        public static CharPtr FromCharArray(char[] value)
        {
            return new CharPtr(value);
        }

        public static CharPtr FromByteArray(byte[] value)
        {
            return new CharPtr(value);
        }

        public CharPtr()
        {
            chars = null;
            index = 0;
        }

        public CharPtr(string str)
        {
            string validated = EnsureArgument(str, nameof(str));
            chars = (validated + '\0').ToCharArray();
            index = 0;
        }

        public CharPtr(CharPtr ptr)
        {
            CharPtr validatedPtr = EnsureArgument(ptr, nameof(ptr));
            chars = validatedPtr.chars;
            index = validatedPtr.index;
        }

        public CharPtr(CharPtr ptr, int index)
        {
            CharPtr validatedPtr = EnsureArgument(ptr, nameof(ptr));
            chars = validatedPtr.chars;
            this.index = index;
        }

        public CharPtr(char[] chars)
        {
            this.chars = EnsureArgument(chars, nameof(chars));
            index = 0;
        }

        public CharPtr(char[] chars, int index)
        {
            this.chars = EnsureArgument(chars, nameof(chars));
            this.index = index;
        }

        public CharPtr(byte[] bytes)
        {
            byte[] validatedBytes = EnsureArgument(bytes, nameof(bytes));
            chars = new char[validatedBytes.Length];
            for (int i = 0; i < validatedBytes.Length; i++)
            {
                chars[i] = (char)validatedBytes[i];
            }

            index = 0;
        }

        public CharPtr(IntPtr ptr)
        {
            chars = Array.Empty<char>();
            index = 0;
        }

        public static CharPtr operator +(CharPtr ptr, int offset)
        {
            CharPtr validated = EnsureArgument(ptr, nameof(ptr));
            return new CharPtr(validated.chars, validated.index + offset);
        }

        public static CharPtr operator -(CharPtr ptr, int offset)
        {
            CharPtr validated = EnsureArgument(ptr, nameof(ptr));
            return new CharPtr(validated.chars, validated.index - offset);
        }

        public static CharPtr operator +(CharPtr ptr, uint offset)
        {
            CharPtr validated = EnsureArgument(ptr, nameof(ptr));
            return new CharPtr(validated.chars, validated.index + (int)offset);
        }

        public static CharPtr operator -(CharPtr ptr, uint offset)
        {
            CharPtr validated = EnsureArgument(ptr, nameof(ptr));
            return new CharPtr(validated.chars, validated.index - (int)offset);
        }

        public static CharPtr Subtract(CharPtr ptr, int offset)
        {
            return ptr - offset;
        }

        public static CharPtr Subtract(CharPtr ptr, uint offset)
        {
            return ptr - offset;
        }

        public static int Subtract(CharPtr left, CharPtr right)
        {
            return left - right;
        }

        public void Inc()
        {
            index++;
        }

        public void Dec()
        {
            index--;
        }

        public CharPtr Next()
        {
            return new CharPtr(chars, index + 1);
        }

        public CharPtr Prev()
        {
            return new CharPtr(chars, index - 1);
        }

        public CharPtr Add(int ofs)
        {
            return new CharPtr(chars, index + ofs);
        }

        public CharPtr Subtract(int ofs)
        {
            return new CharPtr(chars, index - ofs);
        }

        public CharPtr Sub(int ofs)
        {
            return Subtract(ofs);
        }

        public static bool operator ==(CharPtr ptr, char ch)
        {
            return EnsureArgument(ptr, nameof(ptr))[0] == ch;
        }

        public static bool operator ==(char ch, CharPtr ptr)
        {
            return EnsureArgument(ptr, nameof(ptr))[0] == ch;
        }

        public static bool operator !=(CharPtr ptr, char ch)
        {
            return EnsureArgument(ptr, nameof(ptr))[0] != ch;
        }

        public static bool operator !=(char ch, CharPtr ptr)
        {
            return EnsureArgument(ptr, nameof(ptr))[0] != ch;
        }

        public static CharPtr operator +(CharPtr ptr1, CharPtr ptr2)
        {
            EnsureArgument(ptr1, nameof(ptr1));
            EnsureArgument(ptr2, nameof(ptr2));
            string result = "";
            for (int i = 0; ptr1[i] != '\0'; i++)
            {
                result += ptr1[i];
            }

            for (int i = 0; ptr2[i] != '\0'; i++)
            {
                result += ptr2[i];
            }

            return new CharPtr(result);
        }

        public static int operator -(CharPtr ptr1, CharPtr ptr2)
        {
            CharPtr left = EnsureArgument(ptr1, nameof(ptr1));
            CharPtr right = EnsureArgument(ptr2, nameof(ptr2));
            Debug.Assert(left.chars == right.chars);
            return left.index - right.index;
        }

        public static bool operator <(CharPtr ptr1, CharPtr ptr2)
        {
            CharPtr left = EnsureArgument(ptr1, nameof(ptr1));
            CharPtr right = EnsureArgument(ptr2, nameof(ptr2));
            Debug.Assert(left.chars == right.chars);
            return left.index < right.index;
        }

        public static bool operator <=(CharPtr ptr1, CharPtr ptr2)
        {
            CharPtr left = EnsureArgument(ptr1, nameof(ptr1));
            CharPtr right = EnsureArgument(ptr2, nameof(ptr2));
            Debug.Assert(left.chars == right.chars);
            return left.index <= right.index;
        }

        public static bool operator >(CharPtr ptr1, CharPtr ptr2)
        {
            CharPtr left = EnsureArgument(ptr1, nameof(ptr1));
            CharPtr right = EnsureArgument(ptr2, nameof(ptr2));
            Debug.Assert(left.chars == right.chars);
            return left.index > right.index;
        }

        public static bool operator >=(CharPtr ptr1, CharPtr ptr2)
        {
            CharPtr left = EnsureArgument(ptr1, nameof(ptr1));
            CharPtr right = EnsureArgument(ptr2, nameof(ptr2));
            Debug.Assert(left.chars == right.chars);
            return left.index >= right.index;
        }

        public static int Compare(CharPtr left, CharPtr right)
        {
            CharPtr validatedLeft = EnsureArgument(left, nameof(left));
            CharPtr validatedRight = EnsureArgument(right, nameof(right));
            Debug.Assert(validatedLeft.chars == validatedRight.chars);

            if (validatedLeft.index == validatedRight.index)
            {
                return 0;
            }

            return validatedLeft.index < validatedRight.index ? -1 : 1;
        }

        public static bool operator ==(CharPtr ptr1, CharPtr ptr2)
        {
            if (ReferenceEquals(ptr1, ptr2))
            {
                return true;
            }

            if (ptr1 is null || ptr2 is null)
            {
                return false;
            }

            return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index);
        }

        public static bool operator !=(CharPtr ptr1, CharPtr ptr2)
        {
            return !(ptr1 == ptr2);
        }

        public override bool Equals(object obj)
        {
            return this == (obj as CharPtr);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            System.Text.StringBuilder result = new();
            for (int i = index; (i < chars.Length) && (chars[i] != '\0'); i++)
            {
                result.Append(chars[i]);
            }

            return result.ToString();
        }

        public string ToString(int length)
        {
            System.Text.StringBuilder result = new();
            for (int i = index; (i < chars.Length) && i < (length + index); i++)
            {
                result.Append(chars[i]);
            }

            return result.ToString();
        }

        private static T EnsureArgument<T>(T value, string paramName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            return value;
        }
    }
}
