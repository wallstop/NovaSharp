namespace WallstopStudios.NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).
#pragma warning disable CA1720 // Legacy LuaPort identifiers intentionally match upstream pointer naming.
#pragma warning disable CA1725 // Legacy LuaPort overrides keep upstream parameter shapes for readability.

    //
    // This part taken from KopiLua - https://github.com/NLua/KopiLua
    //
    // =======================================================================================
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
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// A lightweight pointer-like structure for efficient character array traversal.
    /// Converted from class to readonly struct to eliminate heap allocations in hot paths.
    /// </summary>
    /// <remarks>
    /// This struct mimics C pointer semantics for KopiLua string operations.
    /// A "null" CharPtr is represented by <see cref="IsNull"/> being true (when chars array is null).
    /// Use <see cref="Null"/> to get a null-equivalent instance.
    /// </remarks>
    public readonly struct CharPtr : IEquatable<CharPtr>
    {
        internal readonly char[] chars;
        internal readonly int index;

        /// <summary>
        /// Gets a null-equivalent CharPtr instance.
        /// </summary>
        public static CharPtr Null => default;

        /// <summary>
        /// Returns true if this CharPtr represents a null pointer (no backing array).
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => chars == null;
        }

        public char this[int offset]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return chars[index + offset]; }
            set { chars[index + offset] = value; }
        }

        public char this[uint offset]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return chars[index + offset]; }
            set { chars[index + offset] = value; }
        }

        public char this[long offset]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return chars[index + (int)offset]; }
            set { chars[index + (int)offset] = value; }
        }

        public static implicit operator CharPtr(string str)
        {
            if (str == null)
            {
                return Null;
            }
            return new CharPtr(str);
        }

        public static implicit operator CharPtr(char[] chars)
        {
            if (chars == null)
            {
                return Null;
            }
            return new CharPtr(chars);
        }

        public static implicit operator CharPtr(byte[] bytes)
        {
            if (bytes == null)
            {
                return Null;
            }
            return new CharPtr(bytes);
        }

        public static CharPtr FromString(string value)
        {
            if (value == null)
            {
                return Null;
            }
            return new CharPtr(value);
        }

        public static CharPtr FromCharArray(char[] value)
        {
            if (value == null)
            {
                return Null;
            }
            return new CharPtr(value);
        }

        public static CharPtr FromByteArray(byte[] value)
        {
            if (value == null)
            {
                return Null;
            }
            return new CharPtr(value);
        }

        public CharPtr(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            chars = (str + '\0').ToCharArray();
            index = 0;
        }

        public CharPtr(CharPtr ptr)
        {
            if (ptr.IsNull)
            {
                throw new ArgumentException("CharPtr cannot be null", nameof(ptr));
            }
            chars = ptr.chars;
            index = ptr.index;
        }

        public CharPtr(CharPtr ptr, int index)
        {
            if (ptr.IsNull)
            {
                throw new ArgumentException("CharPtr cannot be null", nameof(ptr));
            }
            chars = ptr.chars;
            this.index = index;
        }

        public CharPtr(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars));
            }
            this.chars = chars;
            index = 0;
        }

        public CharPtr(char[] chars, int index)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars));
            }
            this.chars = chars;
            this.index = index;
        }

        public CharPtr(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i] = (char)bytes[i];
            }
            index = 0;
        }

        public CharPtr(IntPtr ptr)
        {
            chars = Array.Empty<char>();
            index = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharPtr operator +(CharPtr ptr, int offset)
        {
            return new CharPtr(ptr.chars, ptr.index + offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharPtr operator -(CharPtr ptr, int offset)
        {
            return new CharPtr(ptr.chars, ptr.index - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharPtr operator +(CharPtr ptr, uint offset)
        {
            return new CharPtr(ptr.chars, ptr.index + (int)offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CharPtr operator -(CharPtr ptr, uint offset)
        {
            return new CharPtr(ptr.chars, ptr.index - (int)offset);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharPtr Next()
        {
            return new CharPtr(chars, index + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharPtr Prev()
        {
            return new CharPtr(chars, index - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharPtr Add(int ofs)
        {
            return new CharPtr(chars, index + ofs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharPtr Subtract(int ofs)
        {
            return new CharPtr(chars, index - ofs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharPtr Sub(int ofs)
        {
            return Subtract(ofs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CharPtr ptr, char ch)
        {
            return ptr[0] == ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(char ch, CharPtr ptr)
        {
            return ptr[0] == ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CharPtr ptr, char ch)
        {
            return ptr[0] != ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(char ch, CharPtr ptr)
        {
            return ptr[0] != ch;
        }

        public static CharPtr operator +(CharPtr ptr1, CharPtr ptr2)
        {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int operator -(CharPtr ptr1, CharPtr ptr2)
        {
            Debug.Assert(ptr1.chars == ptr2.chars);
            return ptr1.index - ptr2.index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(CharPtr ptr1, CharPtr ptr2)
        {
            Debug.Assert(ptr1.chars == ptr2.chars);
            return ptr1.index < ptr2.index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(CharPtr ptr1, CharPtr ptr2)
        {
            Debug.Assert(ptr1.chars == ptr2.chars);
            return ptr1.index <= ptr2.index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(CharPtr ptr1, CharPtr ptr2)
        {
            Debug.Assert(ptr1.chars == ptr2.chars);
            return ptr1.index > ptr2.index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(CharPtr ptr1, CharPtr ptr2)
        {
            Debug.Assert(ptr1.chars == ptr2.chars);
            return ptr1.index >= ptr2.index;
        }

        public static int Compare(CharPtr left, CharPtr right)
        {
            Debug.Assert(left.chars == right.chars);

            if (left.index == right.index)
            {
                return 0;
            }

            return left.index < right.index ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CharPtr ptr1, CharPtr ptr2)
        {
            return (ptr1.chars == ptr2.chars) && (ptr1.index == ptr2.index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CharPtr ptr1, CharPtr ptr2)
        {
            return !(ptr1 == ptr2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CharPtr other)
        {
            return (chars == other.chars) && (index == other.index);
        }

        public override bool Equals(object obj)
        {
            return obj is CharPtr other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (chars == null)
            {
                return 0;
            }
            return HashCodeHelper.HashCode(RuntimeHelpers.GetHashCode(chars), index);
        }

        public override string ToString()
        {
            if (chars == null)
            {
                return string.Empty;
            }
            using Cysharp.Text.Utf16ValueStringBuilder result = ZStringBuilder.Create();
            for (int i = index; (i < chars.Length) && (chars[i] != '\0'); i++)
            {
                result.Append(chars[i]);
            }

            return result.ToString();
        }

        public string ToString(int length)
        {
            if (chars == null)
            {
                return string.Empty;
            }
            using Cysharp.Text.Utf16ValueStringBuilder result = ZStringBuilder.Create();
            for (int i = index; (i < chars.Length) && i < (length + index); i++)
            {
                result.Append(chars[i]);
            }

            return result.ToString();
        }
    }
}
