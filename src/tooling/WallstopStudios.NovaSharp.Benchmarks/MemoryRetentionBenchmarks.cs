namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using global::NovaSharp;
    using BenchmarkDotNet.Attributes;

    /// <summary>
    /// Retention-oriented probes for Phase A0.5 memory lifecycle work.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class MemoryRetentionBenchmarks
    {
        private const int BurstCount = 64;
        private const int ScratchIterations = 32;
        private const int CompileTokenBufferLength = 512;
        private const int CompileParseStackLength = 256;
        private const int VmValueStackLength = 128;
        private const int VmFrameStackLength = 64;
        private static readonly object VmSentinel = new();
        private readonly ScratchBufferPool<byte> _compileTokenPool = new();
        private readonly ScratchBufferPool<int> _compileParseStackPool = new();
        private readonly ScratchBufferPool<object> _vmValueStackPool = new();
        private readonly ScratchBufferPool<int> _vmFrameStackPool = new();

        /// <summary>
        /// Compiles many unique chunks, clears reclaimable cache entries, and reports retained bytes.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long CompilationBurstAfterCriticalTrimRetainedBytes()
        {
            using LuaEngine engine = LuaEngine.Create(
                new LuaEngineOptions { EnableScriptCaching = true, ScriptCacheMaxEntries = 8 }
            );

            for (int i = 0; i < BurstCount; i++)
            {
                engine.Run(CreateReturnChunk(i), CreateChunkName(i));
            }

            engine.TrimMemory(LuaMemoryTrimLevel.Critical);
            return engine.GetMemoryStatistics().EstimatedRetainedBytes;
        }

        /// <summary>
        /// Exercises small argument array paths through the public callback facade.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long CallbackBurstAfterCriticalTrimRetainedBytes()
        {
            using LuaEngine engine = LuaEngine.Create();
            engine.Globals["sum"] = engine.CreateCallback(
                static (context, args) =>
                {
                    _ = context;
                    long total = 0L;
                    for (int i = 0; i < args.Length; i++)
                    {
                        total += args[i].AsInteger();
                    }

                    return LuaValue.FromInteger(total);
                },
                "sum"
            );

            for (int i = 0; i < BurstCount; i++)
            {
                engine.Run("return sum(1, 2, 3, 4, 5, 6, 7, 8)", "callback_burst.lua");
            }

            engine.TrimMemory(LuaMemoryTrimLevel.Critical);
            return engine.GetMemoryStatistics().EstimatedRetainedBytes;
        }

        /// <summary>
        /// Captures process working set after a compile burst. This is diagnostic only.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long CompilationBurstWorkingSetBytes()
        {
            using LuaEngine engine = LuaEngine.Create(
                new LuaEngineOptions { EnableScriptCaching = true, ScriptCacheMaxEntries = 8 }
            );

            for (int i = 0; i < BurstCount; i++)
            {
                engine.Run(CreateReturnChunk(i), CreateChunkName(i));
            }

            engine.TrimMemory(LuaMemoryTrimLevel.Critical);
            using Process process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }

        /// <summary>
        /// Benchmark-only compile scratch prototype using one reusable pool per scratch buffer type.
        /// </summary>
        [Benchmark]
        public long CompileScratchPoolPerBufferPrototype()
        {
            long checksum = 0L;
            for (int i = 0; i < ScratchIterations; i++)
            {
                byte[] tokenBuffer = _compileTokenPool.Rent(CompileTokenBufferLength);
                int[] parseStack = _compileParseStackPool.Rent(CompileParseStackLength);
                try
                {
                    checksum += RunCompileScratchWork(tokenBuffer, parseStack, i);
                }
                finally
                {
                    _compileTokenPool.Return(tokenBuffer);
                    _compileParseStackPool.Return(parseStack);
                }
            }

            return checksum
                + _compileTokenPool.EstimatedRetainedBytes
                + _compileParseStackPool.EstimatedRetainedBytes;
        }

        /// <summary>
        /// Benchmark-only compile scratch prototype using direct shared array-pool rentals.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long CompileScratchArrayPoolPrototype()
        {
            long checksum = 0L;
            for (int i = 0; i < ScratchIterations; i++)
            {
                byte[] tokenBuffer = ArrayPool<byte>.Shared.Rent(CompileTokenBufferLength);
                int[] parseStack = ArrayPool<int>.Shared.Rent(CompileParseStackLength);
                try
                {
                    checksum += RunCompileScratchWork(tokenBuffer, parseStack, i);
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(parseStack, clearArray: true);
                    ArrayPool<byte>.Shared.Return(tokenBuffer, clearArray: true);
                }
            }

            return checksum;
        }

        /// <summary>
        /// Benchmark-only compile scratch prototype that releases all scratch buffers at scope end.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long CompileScratchScopePrototype()
        {
            long checksum = 0L;
            for (int i = 0; i < ScratchIterations; i++)
            {
                using ScratchScope scope = new();
                byte[] tokenBuffer = scope.RentBytes(CompileTokenBufferLength);
                int[] parseStack = scope.RentInts(CompileParseStackLength);
                checksum += RunCompileScratchWork(tokenBuffer, parseStack, i);
            }

            return checksum;
        }

        /// <summary>
        /// Benchmark-only VM scratch prototype using one reusable pool per scratch buffer type.
        /// </summary>
        [Benchmark]
        public long VmScratchPoolPerBufferPrototype()
        {
            long checksum = 0L;
            for (int i = 0; i < ScratchIterations; i++)
            {
                object[] valueStack = _vmValueStackPool.Rent(VmValueStackLength);
                int[] frameStack = _vmFrameStackPool.Rent(VmFrameStackLength);
                try
                {
                    checksum += RunVmScratchWork(valueStack, frameStack, i);
                }
                finally
                {
                    _vmValueStackPool.Return(valueStack);
                    _vmFrameStackPool.Return(frameStack);
                }
            }

            return checksum
                + _vmValueStackPool.EstimatedRetainedBytes
                + _vmFrameStackPool.EstimatedRetainedBytes;
        }

        /// <summary>
        /// Benchmark-only VM scratch prototype using direct shared array-pool rentals.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long VmScratchArrayPoolPrototype()
        {
            long checksum = 0L;
            for (int i = 0; i < ScratchIterations; i++)
            {
                object[] valueStack = ArrayPool<object>.Shared.Rent(VmValueStackLength);
                int[] frameStack = ArrayPool<int>.Shared.Rent(VmFrameStackLength);
                try
                {
                    checksum += RunVmScratchWork(valueStack, frameStack, i);
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(frameStack, clearArray: true);
                    ArrayPool<object>.Shared.Return(valueStack, clearArray: true);
                }
            }

            return checksum;
        }

        /// <summary>
        /// Benchmark-only VM scratch prototype that releases all scratch buffers at scope end.
        /// </summary>
        [Benchmark]
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "BenchmarkDotNet discovers instance benchmark methods."
        )]
        public long VmScratchScopePrototype()
        {
            long checksum = 0L;
            for (int i = 0; i < ScratchIterations; i++)
            {
                using ScratchScope scope = new();
                object[] valueStack = scope.RentObjects(VmValueStackLength);
                int[] frameStack = scope.RentInts(VmFrameStackLength);
                checksum += RunVmScratchWork(valueStack, frameStack, i);
            }

            return checksum;
        }

        private static string CreateReturnChunk(int value)
        {
            return string.Concat(
                "return ",
                value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            );
        }

        private static string CreateChunkName(int value)
        {
            return string.Concat(
                "memory_retention_",
                value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ".lua"
            );
        }

        private static long RunCompileScratchWork(byte[] tokenBuffer, int[] parseStack, int seed)
        {
            long checksum = 0L;
            for (int i = 0; i < CompileTokenBufferLength; i++)
            {
                byte value = unchecked((byte)(seed + i));
                tokenBuffer[i] = value;
                checksum += value;
            }

            for (int i = 0; i < CompileParseStackLength; i++)
            {
                int value = seed + i;
                parseStack[i] = value;
                checksum += value;
            }

            return checksum;
        }

        private static long RunVmScratchWork(object[] valueStack, int[] frameStack, int seed)
        {
            long checksum = 0L;
            for (int i = 0; i < VmValueStackLength; i++)
            {
                valueStack[i] = VmSentinel;
                checksum += valueStack[i] == null ? 0 : 1;
            }

            for (int i = 0; i < VmFrameStackLength; i++)
            {
                int value = seed + i;
                frameStack[i] = value;
                checksum += value;
            }

            return checksum;
        }

        private sealed class ScratchBufferPool<T>
        {
            private readonly Stack<T[]> _buffers = new();
            private long _estimatedRetainedBytes;

            internal long EstimatedRetainedBytes
            {
                get { return _estimatedRetainedBytes; }
            }

            internal T[] Rent(int length)
            {
                if (_buffers.Count > 0)
                {
                    T[] buffer = _buffers.Pop();
                    _estimatedRetainedBytes -= EstimateBytes(buffer.Length);
                    if (buffer.Length >= length)
                    {
                        return buffer;
                    }
                }

                return new T[length];
            }

            internal void Return(T[] buffer)
            {
                if (buffer == null)
                {
                    return;
                }

                Array.Clear(buffer, 0, buffer.Length);
                _buffers.Push(buffer);
                _estimatedRetainedBytes += EstimateBytes(buffer.Length);
            }

            private static long EstimateBytes(int length)
            {
                return IntPtr.Size + ((long)length * EstimateElementBytes());
            }

            private static int EstimateElementBytes()
            {
                Type type = typeof(T);
                if (!type.IsValueType)
                {
                    return IntPtr.Size;
                }

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                        return 1;
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        return 2;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Single:
                        return 4;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Double:
                        return 8;
                    case TypeCode.Decimal:
                        return 16;
                    default:
                        return 32;
                }
            }
        }

        private sealed class ScratchScope : IDisposable
        {
            private readonly List<byte[]> _byteArrays = new();
            private readonly List<int[]> _intArrays = new();
            private readonly List<object[]> _objectArrays = new();

            internal byte[] RentBytes(int minimumLength)
            {
                byte[] array = ArrayPool<byte>.Shared.Rent(minimumLength);
                _byteArrays.Add(array);
                return array;
            }

            internal int[] RentInts(int minimumLength)
            {
                int[] array = ArrayPool<int>.Shared.Rent(minimumLength);
                _intArrays.Add(array);
                return array;
            }

            internal object[] RentObjects(int minimumLength)
            {
                object[] array = ArrayPool<object>.Shared.Rent(minimumLength);
                _objectArrays.Add(array);
                return array;
            }

            public void Dispose()
            {
                for (int i = 0; i < _objectArrays.Count; i++)
                {
                    ArrayPool<object>.Shared.Return(_objectArrays[i], clearArray: true);
                }

                for (int i = 0; i < _intArrays.Count; i++)
                {
                    ArrayPool<int>.Shared.Return(_intArrays[i], clearArray: true);
                }

                for (int i = 0; i < _byteArrays.Count; i++)
                {
                    ArrayPool<byte>.Shared.Return(_byteArrays[i], clearArray: true);
                }
            }
        }
    }
}
