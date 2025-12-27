namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.StringLib
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements Lua 5.3+ string.pack, string.unpack, and string.packsize functions (§6.4.2).
    /// These functions provide binary serialization and deserialization capabilities.
    /// </summary>
    /// <remarks>
    /// Format string options:
    /// &lt; - Little endian
    /// &gt; - Big endian
    /// = - Native endian
    /// ![n] - Set maximum alignment to n (default is native alignment)
    /// b - Signed byte
    /// B - Unsigned byte
    /// h - Signed short (native size)
    /// H - Unsigned short (native size)
    /// l - Signed long (native size)
    /// L - Unsigned long (native size)
    /// j - lua_Integer (8 bytes)
    /// J - lua_Unsigned (8 bytes)
    /// T - size_t (platform dependent, typically 8 bytes)
    /// i[n] - Signed int with n bytes (default is native int size)
    /// I[n] - Unsigned int with n bytes (default is native int size)
    /// f - Float (4 bytes)
    /// d - Double (8 bytes)
    /// n - lua_Number (8 bytes, same as double)
    /// cn - Fixed-size string with n bytes (padding with zeros)
    /// z - Zero-terminated string
    /// s[n] - String preceded by its length coded as unsigned int with n bytes (default 8)
    /// x - One byte of padding (zero)
    /// Xop - Empty item that aligns according to option op
    /// ' ' (space) - Ignored
    /// </remarks>
    [NovaSharpModule(Namespace = "string")]
    internal static class StringPackModule
    {
        // Native sizes for platform-dependent types
        private const int NativeShortSize = sizeof(short);
        private const int NativeLongSize = sizeof(long);
        private const int NativeIntSize = sizeof(int);
        private const int NativeSizeT = sizeof(ulong);
        private const int LuaIntegerSize = 8;
        private const int MaxStringPackSize = 16; // Maximum size for 'i'/'I' specifiers
        private const int DefaultAlignment = 1; // Default is no alignment
        private const int InitialBufferSize = 256; // Initial pooled buffer size
        private const int MaxResultsCapacity = 64; // Maximum expected unpack results

        /// <summary>
        /// Implements Lua `string.pack`, returning a binary string containing the packed values.
        /// Available in Lua 5.3+.
        /// </summary>
        [NovaSharpModuleMethod(Name = "pack")]
        public static DynValue Pack(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            LuaVersionGuard.ThrowIfUnavailable(
                executionContext.Script.CompatibilityVersion,
                LuaCompatibilityVersion.Lua53,
                "string.pack"
            );

            DynValue fmtArg = args.AsType(0, "pack", DataType.String, false);
            string fmt = fmtArg.String;

            // Estimate buffer size based on format string
            int estimatedSize = EstimatePackSize(fmt, args.Count - 1);

            // Use pooled buffer - may be larger than requested
            using PooledResource<byte[]> pooled = SystemArrayPool<byte>.Get(
                estimatedSize,
                clearOnReturn: false,
                out byte[] buffer
            );
            int writePos = 0;
            int capacity = buffer.Length;

            int argIndex = 1;
            bool littleEndian = BitConverter.IsLittleEndian; // Default to native
            int maxAlignment = DefaultAlignment;

            for (int i = 0; i < fmt.Length; i++)
            {
                char c = fmt[i];

                switch (c)
                {
                    case '<':
                        littleEndian = true;
                        break;

                    case '>':
                        littleEndian = false;
                        break;

                    case '=':
                        littleEndian = BitConverter.IsLittleEndian;
                        break;

                    case '!':
                        maxAlignment = ParseOptionalSize(fmt, ref i, DefaultAlignment);
                        if (maxAlignment < 1 || maxAlignment > 16)
                        {
                            throw new ScriptRuntimeException("alignment must be between 1 and 16");
                        }
                        break;

                    case 'b': // Signed byte
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        ValidateSignedRange(value, 1);
                        EnsureCapacity(ref buffer, ref capacity, writePos, 1);
                        buffer[writePos++] = (byte)(sbyte)value;
                        break;
                    }

                    case 'B': // Unsigned byte
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        ValidateUnsignedRange(value, 1);
                        EnsureCapacity(ref buffer, ref capacity, writePos, 1);
                        buffer[writePos++] = (byte)value;
                        break;
                    }

                    case 'h': // Signed short (native)
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        ValidateSignedRange(value, NativeShortSize);
                        EnsureCapacity(ref buffer, ref capacity, writePos, NativeShortSize);
                        WriteInteger(buffer, ref writePos, value, NativeShortSize, littleEndian);
                        break;
                    }

                    case 'H': // Unsigned short (native)
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        ValidateUnsignedRange(value, NativeShortSize);
                        EnsureCapacity(ref buffer, ref capacity, writePos, NativeShortSize);
                        WriteInteger(buffer, ref writePos, value, NativeShortSize, littleEndian);
                        break;
                    }

                    case 'l': // Signed long (native)
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        EnsureCapacity(ref buffer, ref capacity, writePos, NativeLongSize);
                        WriteInteger(buffer, ref writePos, value, NativeLongSize, littleEndian);
                        break;
                    }

                    case 'L': // Unsigned long (native)
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        EnsureCapacity(ref buffer, ref capacity, writePos, NativeLongSize);
                        WriteInteger(buffer, ref writePos, value, NativeLongSize, littleEndian);
                        break;
                    }

                    case 'j': // lua_Integer
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        EnsureCapacity(ref buffer, ref capacity, writePos, LuaIntegerSize);
                        WriteInteger(buffer, ref writePos, value, LuaIntegerSize, littleEndian);
                        break;
                    }

                    case 'J': // lua_Unsigned
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        EnsureCapacity(ref buffer, ref capacity, writePos, LuaIntegerSize);
                        WriteInteger(buffer, ref writePos, value, LuaIntegerSize, littleEndian);
                        break;
                    }

                    case 'T': // size_t
                    {
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        if (value < 0)
                        {
                            throw new ScriptRuntimeException(
                                ZString.Concat(
                                    "bad argument #",
                                    argIndex,
                                    " to 'pack' (unsigned overflow)"
                                )
                            );
                        }
                        EnsureCapacity(ref buffer, ref capacity, writePos, NativeSizeT);
                        WriteInteger(buffer, ref writePos, value, NativeSizeT, littleEndian);
                        break;
                    }

                    case 'i': // Signed int with optional size
                    {
                        int size = ParseOptionalSize(fmt, ref i, NativeIntSize);
                        ValidateIntegerSize(size);
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        ValidateSignedRange(value, size);
                        EnsureCapacity(ref buffer, ref capacity, writePos, size);
                        WriteInteger(buffer, ref writePos, value, size, littleEndian);
                        break;
                    }

                    case 'I': // Unsigned int with optional size
                    {
                        int size = ParseOptionalSize(fmt, ref i, NativeIntSize);
                        ValidateIntegerSize(size);
                        long value = GetIntegerArg(args, argIndex++, "pack");
                        ValidateUnsignedRange(value, size);
                        EnsureCapacity(ref buffer, ref capacity, writePos, size);
                        WriteInteger(buffer, ref writePos, value, size, littleEndian);
                        break;
                    }

                    case 'f': // Float
                    {
                        double value = GetNumberArg(args, argIndex++, "pack");
                        float floatVal = (float)value;
                        EnsureCapacity(ref buffer, ref capacity, writePos, 4);
                        WriteFloat(buffer, ref writePos, floatVal, littleEndian);
                        break;
                    }

                    case 'd': // Double
                    case 'n': // lua_Number (same as double)
                    {
                        double value = GetNumberArg(args, argIndex++, "pack");
                        EnsureCapacity(ref buffer, ref capacity, writePos, 8);
                        WriteDouble(buffer, ref writePos, value, littleEndian);
                        break;
                    }

                    case 'c': // Fixed-size string
                    {
                        int size = ParseRequiredSize(fmt, ref i, "pack");
                        string str = GetStringArg(args, argIndex++, "pack");
                        if (str.Length > size)
                        {
                            throw new ScriptRuntimeException("string longer than given size");
                        }
                        EnsureCapacity(ref buffer, ref capacity, writePos, size);
                        WriteStringBytes(buffer, ref writePos, str);
                        // Pad with zeros
                        int padCount = size - str.Length;
                        for (int pad = 0; pad < padCount; pad++)
                        {
                            buffer[writePos++] = 0;
                        }
                        break;
                    }

                    case 'z': // Zero-terminated string
                    {
                        string str = GetStringArg(args, argIndex++, "pack");
                        if (ContainsNullChar(str))
                        {
                            throw new ScriptRuntimeException("string contains zeros");
                        }
                        EnsureCapacity(ref buffer, ref capacity, writePos, str.Length + 1);
                        WriteStringBytes(buffer, ref writePos, str);
                        buffer[writePos++] = 0;
                        break;
                    }

                    case 's': // Length-prefixed string
                    {
                        int size = ParseOptionalSize(fmt, ref i, NativeSizeT);
                        ValidateIntegerSize(size);
                        string str = GetStringArg(args, argIndex++, "pack");
                        long length = str.Length;
                        ValidateUnsignedRange(length, size);
                        EnsureCapacity(ref buffer, ref capacity, writePos, size + str.Length);
                        WriteInteger(buffer, ref writePos, length, size, littleEndian);
                        WriteStringBytes(buffer, ref writePos, str);
                        break;
                    }

                    case 'x': // Padding byte
                        EnsureCapacity(ref buffer, ref capacity, writePos, 1);
                        buffer[writePos++] = 0;
                        break;

                    case 'X': // Alignment padding
                    {
                        if (i + 1 >= fmt.Length)
                        {
                            throw new ScriptRuntimeException("invalid format option 'X'");
                        }
                        char alignOp = fmt[++i];
                        int alignSize = GetAlignmentSize(alignOp, fmt, ref i);
                        int alignment = Math.Min(alignSize, maxAlignment);
                        int padding = (alignment - (writePos % alignment)) % alignment;
                        EnsureCapacity(ref buffer, ref capacity, writePos, padding);
                        for (int p = 0; p < padding; p++)
                        {
                            buffer[writePos++] = 0;
                        }
                        break;
                    }

                    case ' ':
                        // Ignored
                        break;

                    default:
                        throw new ScriptRuntimeException(
                            ZString.Concat("invalid format option '", c, "'")
                        );
                }
            }

            // Convert bytes to a Lua string (ISO-8859-1 encoding where each byte = one char)
            return DynValue.NewString(BytesToLuaString(buffer, writePos));
        }

        /// <summary>
        /// Implements Lua `string.unpack`, returning the unpacked values from a binary string.
        /// Available in Lua 5.3+.
        /// </summary>
        [NovaSharpModuleMethod(Name = "unpack")]
        public static DynValue Unpack(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            LuaVersionGuard.ThrowIfUnavailable(
                executionContext.Script.CompatibilityVersion,
                LuaCompatibilityVersion.Lua53,
                "string.unpack"
            );

            DynValue fmtArg = args.AsType(0, "unpack", DataType.String, false);
            DynValue dataArg = args.AsType(1, "unpack", DataType.String, false);
            DynValue posArg = args.AsType(2, "unpack", DataType.Number, true);

            string fmt = fmtArg.String;
            string data = dataArg.String;
            int pos = posArg.IsNil() ? 0 : (int)posArg.Number - 1; // Lua uses 1-based indexing

            if (pos < 0 || pos > data.Length)
            {
                throw new ScriptRuntimeException("initial position out of string");
            }

            // Estimate result count based on format - use pooled DynValue array
            int estimatedResults = EstimateResultCount(fmt);
            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                Math.Max(estimatedResults + 1, MaxResultsCapacity),
                out DynValue[] resultsBuffer
            );
            int resultCount = 0;
            int resultsCapacity = resultsBuffer.Length;

            bool littleEndian = BitConverter.IsLittleEndian;
            int maxAlignment = DefaultAlignment;

            for (int i = 0; i < fmt.Length; i++)
            {
                char c = fmt[i];

                switch (c)
                {
                    case '<':
                        littleEndian = true;
                        break;

                    case '>':
                        littleEndian = false;
                        break;

                    case '=':
                        littleEndian = BitConverter.IsLittleEndian;
                        break;

                    case '!':
                        maxAlignment = ParseOptionalSize(fmt, ref i, DefaultAlignment);
                        break;

                    case 'b': // Signed byte
                        CheckAvailable(data, pos, 1);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber((sbyte)data[pos++]);
                        break;

                    case 'B': // Unsigned byte
                        CheckAvailable(data, pos, 1);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber((byte)data[pos++]);
                        break;

                    case 'h': // Signed short
                        CheckAvailable(data, pos, NativeShortSize);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadSignedInteger(data, ref pos, NativeShortSize, littleEndian)
                        );
                        break;

                    case 'H': // Unsigned short
                        CheckAvailable(data, pos, NativeShortSize);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadUnsignedInteger(data, ref pos, NativeShortSize, littleEndian)
                        );
                        break;

                    case 'l': // Signed long
                        CheckAvailable(data, pos, NativeLongSize);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadSignedInteger(data, ref pos, NativeLongSize, littleEndian)
                        );
                        break;

                    case 'L': // Unsigned long
                        CheckAvailable(data, pos, NativeLongSize);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadUnsignedInteger(data, ref pos, NativeLongSize, littleEndian)
                        );
                        break;

                    case 'j': // lua_Integer
                        CheckAvailable(data, pos, LuaIntegerSize);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadSignedInteger(data, ref pos, LuaIntegerSize, littleEndian)
                        );
                        break;

                    case 'J': // lua_Unsigned
                        CheckAvailable(data, pos, LuaIntegerSize);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadUnsignedInteger(data, ref pos, LuaIntegerSize, littleEndian)
                        );
                        break;

                    case 'T': // size_t
                        CheckAvailable(data, pos, NativeSizeT);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadUnsignedInteger(data, ref pos, NativeSizeT, littleEndian)
                        );
                        break;

                    case 'i': // Signed int with optional size
                    {
                        int size = ParseOptionalSize(fmt, ref i, NativeIntSize);
                        ValidateIntegerSize(size);
                        CheckAvailable(data, pos, size);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadSignedInteger(data, ref pos, size, littleEndian)
                        );
                        break;
                    }

                    case 'I': // Unsigned int with optional size
                    {
                        int size = ParseOptionalSize(fmt, ref i, NativeIntSize);
                        ValidateIntegerSize(size);
                        CheckAvailable(data, pos, size);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadUnsignedInteger(data, ref pos, size, littleEndian)
                        );
                        break;
                    }

                    case 'f': // Float
                        CheckAvailable(data, pos, 4);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadFloat(data, ref pos, littleEndian)
                        );
                        break;

                    case 'd': // Double
                    case 'n': // lua_Number
                        CheckAvailable(data, pos, 8);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewNumber(
                            ReadDouble(data, ref pos, littleEndian)
                        );
                        break;

                    case 'c': // Fixed-size string
                    {
                        int size = ParseRequiredSize(fmt, ref i, "unpack");
                        CheckAvailable(data, pos, size);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewString(
                            data.Substring(pos, size)
                        );
                        pos += size;
                        break;
                    }

                    case 'z': // Zero-terminated string
                    {
                        int nullPos = data.IndexOf('\0', pos);
                        if (nullPos < 0)
                        {
                            throw new ScriptRuntimeException("unfinished string for format 'z'");
                        }
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewString(
                            data.Substring(pos, nullPos - pos)
                        );
                        pos = nullPos + 1;
                        break;
                    }

                    case 's': // Length-prefixed string
                    {
                        int size = ParseOptionalSize(fmt, ref i, NativeSizeT);
                        ValidateIntegerSize(size);
                        CheckAvailable(data, pos, size);
                        long length = ReadUnsignedInteger(data, ref pos, size, littleEndian);
                        if (length > int.MaxValue)
                        {
                            throw new ScriptRuntimeException("string length overflow");
                        }
                        CheckAvailable(data, pos, (int)length);
                        EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
                        resultsBuffer[resultCount++] = DynValue.NewString(
                            data.Substring(pos, (int)length)
                        );
                        pos += (int)length;
                        break;
                    }

                    case 'x': // Skip padding byte
                        CheckAvailable(data, pos, 1);
                        pos++;
                        break;

                    case 'X': // Alignment skip
                    {
                        if (i + 1 >= fmt.Length)
                        {
                            throw new ScriptRuntimeException("invalid format option 'X'");
                        }
                        char alignOp = fmt[++i];
                        int alignSize = GetAlignmentSize(alignOp, fmt, ref i);
                        int alignment = Math.Min(alignSize, maxAlignment);
                        int padding = (alignment - (pos % alignment)) % alignment;
                        CheckAvailable(data, pos, padding);
                        pos += padding;
                        break;
                    }

                    case ' ':
                        // Ignored
                        break;

                    default:
                        throw new ScriptRuntimeException(
                            ZString.Concat("invalid format option '", c, "'")
                        );
                }
            }

            // Add the final position (1-based)
            EnsureResultsCapacity(ref resultsBuffer, resultCount, ref resultsCapacity);
            resultsBuffer[resultCount++] = DynValue.NewNumber(pos + 1);

            // Create final results array
            DynValue[] finalResults = new DynValue[resultCount];
            Array.Copy(resultsBuffer, finalResults, resultCount);
            return DynValue.NewTuple(finalResults);
        }

        /// <summary>
        /// Implements Lua `string.packsize`, returning the size of the packed format.
        /// Available in Lua 5.3+.
        /// </summary>
        [NovaSharpModuleMethod(Name = "packsize")]
        public static DynValue PackSize(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            LuaVersionGuard.ThrowIfUnavailable(
                executionContext.Script.CompatibilityVersion,
                LuaCompatibilityVersion.Lua53,
                "string.packsize"
            );

            DynValue fmtArg = args.AsType(0, "packsize", DataType.String, false);
            string fmt = fmtArg.String;

            int size = 0;
            int maxAlignment = DefaultAlignment;

            for (int i = 0; i < fmt.Length; i++)
            {
                char c = fmt[i];

                switch (c)
                {
                    case '<':
                    case '>':
                    case '=':
                        // Endianness doesn't affect size
                        break;

                    case '!':
                        maxAlignment = ParseOptionalSize(fmt, ref i, DefaultAlignment);
                        break;

                    case 'b':
                    case 'B':
                        size += 1;
                        break;

                    case 'h':
                    case 'H':
                        size += NativeShortSize;
                        break;

                    case 'l':
                    case 'L':
                        size += NativeLongSize;
                        break;

                    case 'j':
                    case 'J':
                        size += LuaIntegerSize;
                        break;

                    case 'T':
                        size += NativeSizeT;
                        break;

                    case 'i':
                    case 'I':
                    {
                        int intSize = ParseOptionalSize(fmt, ref i, NativeIntSize);
                        ValidateIntegerSize(intSize);
                        size += intSize;
                        break;
                    }

                    case 'f':
                        size += 4;
                        break;

                    case 'd':
                    case 'n':
                        size += 8;
                        break;

                    case 'c':
                    {
                        int strSize = ParseRequiredSize(fmt, ref i, "packsize");
                        size += strSize;
                        break;
                    }

                    case 'z':
                    case 's':
                        throw new ScriptRuntimeException("variable-length format");

                    case 'x':
                        size += 1;
                        break;

                    case 'X':
                    {
                        if (i + 1 >= fmt.Length)
                        {
                            throw new ScriptRuntimeException("invalid format option 'X'");
                        }
                        char alignOp = fmt[++i];
                        int alignSize = GetAlignmentSize(alignOp, fmt, ref i);
                        int alignment = Math.Min(alignSize, maxAlignment);
                        int padding = (alignment - (size % alignment)) % alignment;
                        size += padding;
                        break;
                    }

                    case ' ':
                        // Ignored
                        break;

                    default:
                        throw new ScriptRuntimeException(
                            ZString.Concat("invalid format option '", c, "'")
                        );
                }
            }

            return DynValue.NewNumber(size);
        }

        #region Helper Methods

        /// <summary>
        /// Estimates the buffer size needed for a pack operation based on format string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EstimatePackSize(string fmt, int argCount)
        {
            // Quick estimate: 8 bytes per argument plus some overhead for alignment
            int estimate = argCount * 8 + 16;
            // Add extra for potential strings
            return Math.Max(estimate, InitialBufferSize);
        }

        /// <summary>
        /// Estimates the number of results from an unpack operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EstimateResultCount(string fmt)
        {
            int count = 0;
            for (int i = 0; i < fmt.Length; i++)
            {
                char c = fmt[i];
                // Count format specifiers that produce results
                if (
                    c == 'b'
                    || c == 'B'
                    || c == 'h'
                    || c == 'H'
                    || c == 'l'
                    || c == 'L'
                    || c == 'j'
                    || c == 'J'
                    || c == 'T'
                    || c == 'i'
                    || c == 'I'
                    || c == 'f'
                    || c == 'd'
                    || c == 'n'
                    || c == 'c'
                    || c == 'z'
                    || c == 's'
                )
                {
                    count++;
                }
            }
            return count + 1; // +1 for final position
        }

        /// <summary>
        /// Ensures the buffer has enough capacity, growing if needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureCapacity(
            ref byte[] buffer,
            ref int capacity,
            int writePos,
            int needed
        )
        {
            int requiredSize = writePos + needed;
            if (requiredSize <= capacity)
            {
                return;
            }

            // Need to grow the buffer
            int newSize = Math.Max(capacity * 2, requiredSize);
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, writePos);
            ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
            buffer = newBuffer;
            capacity = newBuffer.Length;
        }

        /// <summary>
        /// Ensures the results buffer has enough capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureResultsCapacity(
            ref DynValue[] results,
            int count,
            ref int capacity
        )
        {
            if (count < capacity)
            {
                return;
            }

            // Need to grow - allocate a larger array
            int newSize = capacity * 2;
            DynValue[] newResults = new DynValue[newSize];
            Array.Copy(results, newResults, count);
            results = newResults;
            capacity = newSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseOptionalSize(string fmt, ref int index, int defaultSize)
        {
            int size = 0;
            while (index + 1 < fmt.Length && char.IsDigit(fmt[index + 1]))
            {
                size = size * 10 + (fmt[++index] - '0');
            }
            return size == 0 ? defaultSize : size;
        }

        private static int ParseRequiredSize(string fmt, ref int index, string funcName)
        {
            int size = 0;
            while (index + 1 < fmt.Length && char.IsDigit(fmt[index + 1]))
            {
                size = size * 10 + (fmt[++index] - '0');
            }
            if (size == 0)
            {
                throw new ScriptRuntimeException(
                    ZString.Concat("missing size for format option 'c' in '", funcName, "'")
                );
            }
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateIntegerSize(int size)
        {
            if (size < 1 || size > MaxStringPackSize)
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "integral size (",
                        size,
                        ") out of limits [1,",
                        MaxStringPackSize,
                        "]"
                    )
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateSignedRange(long value, int byteCount)
        {
            if (byteCount >= 8)
            {
                return; // 8 bytes can hold any long
            }
            long min = -(1L << (byteCount * 8 - 1));
            long max = (1L << (byteCount * 8 - 1)) - 1;
            if (value < min || value > max)
            {
                throw new ScriptRuntimeException("integer overflow");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateUnsignedRange(long value, int byteCount)
        {
            if (value < 0)
            {
                throw new ScriptRuntimeException("unsigned overflow");
            }
            if (byteCount >= 8)
            {
                return; // 8 bytes can hold any positive long
            }
            long max = (1L << (byteCount * 8)) - 1;
            if (value > max)
            {
                throw new ScriptRuntimeException("unsigned overflow");
            }
        }

        private static long GetIntegerArg(CallbackArguments args, int index, string funcName)
        {
            DynValue arg = args.AsType(index, funcName, DataType.Number, false);
            LuaNumber num = arg.LuaNumber;

            // Integer subtype always has integer representation
            if (num.IsInteger)
            {
                return num.AsInteger;
            }

            // Float subtype: check if it can be exactly converted
            double floatValue = num.AsFloat;

            // NaN and infinity cannot be integers
            if (double.IsNaN(floatValue) || double.IsInfinity(floatValue))
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        index + 1,
                        " to '",
                        funcName,
                        "' (number has no integer representation)"
                    )
                );
            }

            // Check if it's a whole number
            double floored = Math.Floor(floatValue);
            if (floored != floatValue)
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        index + 1,
                        " to '",
                        funcName,
                        "' (number has no integer representation)"
                    )
                );
            }

            // Check valid range for long
            const double TwoPow63 = 9223372036854775808.0;
            if (floatValue < -TwoPow63 || floatValue >= TwoPow63)
            {
                throw new ScriptRuntimeException(
                    ZString.Concat(
                        "bad argument #",
                        index + 1,
                        " to '",
                        funcName,
                        "' (number has no integer representation)"
                    )
                );
            }

            return (long)floatValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetNumberArg(CallbackArguments args, int index, string funcName)
        {
            DynValue arg = args.AsType(index, funcName, DataType.Number, false);
            return arg.Number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetStringArg(CallbackArguments args, int index, string funcName)
        {
            DynValue arg = args.AsType(index, funcName, DataType.String, false);
            return arg.String;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckAvailable(string data, int pos, int needed)
        {
            if (pos + needed > data.Length)
            {
                throw new ScriptRuntimeException("data string too short");
            }
        }

        /// <summary>
        /// Checks if a string contains the null character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ContainsNullChar(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\0')
                {
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetAlignmentSize(char op, string fmt, ref int index)
        {
            switch (op)
            {
                case 'b':
                case 'B':
                    return 1;
                case 'h':
                case 'H':
                    return NativeShortSize;
                case 'l':
                case 'L':
                    return NativeLongSize;
                case 'j':
                case 'J':
                    return LuaIntegerSize;
                case 'T':
                    return NativeSizeT;
                case 'i':
                case 'I':
                    return ParseOptionalSize(fmt, ref index, NativeIntSize);
                case 'f':
                    return 4;
                case 'd':
                case 'n':
                    return 8;
                default:
                    throw new ScriptRuntimeException(
                        ZString.Concat("invalid format option 'X", op, "'")
                    );
            }
        }

        /// <summary>
        /// Writes an integer value to the buffer in the specified endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteInteger(
            byte[] buffer,
            ref int writePos,
            long value,
            int byteCount,
            bool littleEndian
        )
        {
            if (littleEndian)
            {
                for (int b = 0; b < byteCount; b++)
                {
                    buffer[writePos++] = (byte)(value >> (b * 8));
                }
            }
            else
            {
                for (int b = byteCount - 1; b >= 0; b--)
                {
                    buffer[writePos++] = (byte)(value >> (b * 8));
                }
            }
        }

        /// <summary>
        /// Writes a float value to the buffer using direct bit manipulation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteFloat(
            byte[] buffer,
            ref int writePos,
            float value,
            bool littleEndian
        )
        {
            // Use union struct to avoid byte[] allocation
            int intBits = FloatIntUnion.SingleToInt32Bits(value);
            if (BitConverter.IsLittleEndian != littleEndian)
            {
                intBits = ReverseBytes32(intBits);
            }
            if (littleEndian)
            {
                buffer[writePos++] = (byte)intBits;
                buffer[writePos++] = (byte)(intBits >> 8);
                buffer[writePos++] = (byte)(intBits >> 16);
                buffer[writePos++] = (byte)(intBits >> 24);
            }
            else
            {
                buffer[writePos++] = (byte)(intBits >> 24);
                buffer[writePos++] = (byte)(intBits >> 16);
                buffer[writePos++] = (byte)(intBits >> 8);
                buffer[writePos++] = (byte)intBits;
            }
        }

        /// <summary>
        /// Writes a double value to the buffer using direct bit manipulation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteDouble(
            byte[] buffer,
            ref int writePos,
            double value,
            bool littleEndian
        )
        {
            long longBits = BitConverter.DoubleToInt64Bits(value);
            if (BitConverter.IsLittleEndian != littleEndian)
            {
                longBits = ReverseBytes64(longBits);
            }
            if (littleEndian)
            {
                buffer[writePos++] = (byte)longBits;
                buffer[writePos++] = (byte)(longBits >> 8);
                buffer[writePos++] = (byte)(longBits >> 16);
                buffer[writePos++] = (byte)(longBits >> 24);
                buffer[writePos++] = (byte)(longBits >> 32);
                buffer[writePos++] = (byte)(longBits >> 40);
                buffer[writePos++] = (byte)(longBits >> 48);
                buffer[writePos++] = (byte)(longBits >> 56);
            }
            else
            {
                buffer[writePos++] = (byte)(longBits >> 56);
                buffer[writePos++] = (byte)(longBits >> 48);
                buffer[writePos++] = (byte)(longBits >> 40);
                buffer[writePos++] = (byte)(longBits >> 32);
                buffer[writePos++] = (byte)(longBits >> 24);
                buffer[writePos++] = (byte)(longBits >> 16);
                buffer[writePos++] = (byte)(longBits >> 8);
                buffer[writePos++] = (byte)longBits;
            }
        }

        /// <summary>
        /// Writes string bytes to buffer (ISO-8859-1 encoding where each char = one byte).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteStringBytes(byte[] buffer, ref int writePos, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                buffer[writePos++] = (byte)str[j];
            }
        }

        /// <summary>
        /// Converts bytes to a Lua string (ISO-8859-1: each byte = one char with same value).
        /// Uses stackalloc for small strings to avoid allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string BytesToLuaString(byte[] bytes, int length)
        {
            if (length == 0)
            {
                return string.Empty;
            }

            // For small strings, use stackalloc to avoid char[] allocation
            if (length <= 256)
            {
                Span<char> chars = stackalloc char[length];
                for (int j = 0; j < length; j++)
                {
                    chars[j] = (char)bytes[j];
                }
                return new string(chars);
            }

            // For larger strings, use pooled char array
            using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(
                length,
                clearOnReturn: false,
                out char[] charBuffer
            );
            for (int j = 0; j < length; j++)
            {
                charBuffer[j] = (char)bytes[j];
            }
            return new string(charBuffer, 0, length);
        }

        /// <summary>
        /// Reads a signed integer from the data string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadSignedInteger(
            string data,
            ref int pos,
            int byteCount,
            bool littleEndian
        )
        {
            long result = 0;
            if (littleEndian)
            {
                for (int b = 0; b < byteCount; b++)
                {
                    result |= (long)(byte)data[pos++] << (b * 8);
                }
                // Sign extend if necessary
                if (byteCount < 8 && (result & (1L << (byteCount * 8 - 1))) != 0)
                {
                    result |= -1L << (byteCount * 8);
                }
            }
            else
            {
                for (int b = byteCount - 1; b >= 0; b--)
                {
                    result |= (long)(byte)data[pos++] << (b * 8);
                }
                // Sign extend if necessary
                if (byteCount < 8 && (result & (1L << (byteCount * 8 - 1))) != 0)
                {
                    result |= -1L << (byteCount * 8);
                }
            }
            return result;
        }

        /// <summary>
        /// Reads an unsigned integer from the data string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadUnsignedInteger(
            string data,
            ref int pos,
            int byteCount,
            bool littleEndian
        )
        {
            long result = 0;
            if (littleEndian)
            {
                for (int b = 0; b < byteCount; b++)
                {
                    result |= (long)(byte)data[pos++] << (b * 8);
                }
            }
            else
            {
                for (int b = byteCount - 1; b >= 0; b--)
                {
                    result |= (long)(byte)data[pos++] << (b * 8);
                }
            }
            return result;
        }

        /// <summary>
        /// Reads a float from the data string using direct bit manipulation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ReadFloat(string data, ref int pos, bool littleEndian)
        {
            int intBits;
            if (littleEndian)
            {
                intBits =
                    (byte)data[pos++]
                    | ((byte)data[pos++] << 8)
                    | ((byte)data[pos++] << 16)
                    | ((byte)data[pos++] << 24);
            }
            else
            {
                intBits =
                    ((byte)data[pos++] << 24)
                    | ((byte)data[pos++] << 16)
                    | ((byte)data[pos++] << 8)
                    | (byte)data[pos++];
            }
            if (BitConverter.IsLittleEndian != littleEndian)
            {
                intBits = ReverseBytes32(intBits);
            }
            return FloatIntUnion.Int32BitsToSingle(intBits);
        }

        /// <summary>
        /// Reads a double from the data string using direct bit manipulation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ReadDouble(string data, ref int pos, bool littleEndian)
        {
            long longBits;
            if (littleEndian)
            {
                longBits =
                    (byte)data[pos++]
                    | ((long)(byte)data[pos++] << 8)
                    | ((long)(byte)data[pos++] << 16)
                    | ((long)(byte)data[pos++] << 24)
                    | ((long)(byte)data[pos++] << 32)
                    | ((long)(byte)data[pos++] << 40)
                    | ((long)(byte)data[pos++] << 48)
                    | ((long)(byte)data[pos++] << 56);
            }
            else
            {
                longBits =
                    ((long)(byte)data[pos++] << 56)
                    | ((long)(byte)data[pos++] << 48)
                    | ((long)(byte)data[pos++] << 40)
                    | ((long)(byte)data[pos++] << 32)
                    | ((long)(byte)data[pos++] << 24)
                    | ((long)(byte)data[pos++] << 16)
                    | ((long)(byte)data[pos++] << 8)
                    | (byte)data[pos++];
            }
            if (BitConverter.IsLittleEndian != littleEndian)
            {
                longBits = ReverseBytes64(longBits);
            }
            return BitConverter.Int64BitsToDouble(longBits);
        }

        /// <summary>
        /// Reverses the byte order of a 32-bit integer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReverseBytes32(int value)
        {
            return ((value & 0x000000FF) << 24)
                | ((value & 0x0000FF00) << 8)
                | ((value & 0x00FF0000) >> 8)
                | (int)((uint)(value & unchecked((int)0xFF000000)) >> 24);
        }

        /// <summary>
        /// Reverses the byte order of a 64-bit integer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReverseBytes64(long value)
        {
            return ((value & 0x00000000000000FFL) << 56)
                | ((value & 0x000000000000FF00L) << 40)
                | ((value & 0x0000000000FF0000L) << 24)
                | ((value & 0x00000000FF000000L) << 8)
                | ((value & 0x000000FF00000000L) >> 8)
                | ((value & 0x0000FF0000000000L) >> 24)
                | ((value & 0x00FF000000000000L) >> 40)
                | (long)((ulong)(value & unchecked((long)0xFF00000000000000L)) >> 56);
        }

        #endregion

        #region FloatIntUnion

        /// <summary>
        /// Union struct for converting between float and int without allocation.
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Explicit
        )]
        private struct FloatIntUnion
        {
            [System.Runtime.InteropServices.FieldOffset(0)]
            public float floatValue;

            [System.Runtime.InteropServices.FieldOffset(0)]
            public int intValue;

            /// <summary>
            /// Converts a float to its raw bit representation.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int SingleToInt32Bits(float value)
            {
                FloatIntUnion union = default;
                union.floatValue = value;
                return union.intValue;
            }

            /// <summary>
            /// Converts raw bits to a float.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float Int32BitsToSingle(int value)
            {
                FloatIntUnion union = default;
                union.intValue = value;
                return union.floatValue;
            }
        }

        #endregion
    }
}
