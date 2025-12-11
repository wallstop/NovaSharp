namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    public sealed class ProcessorBinaryDumpTUnitTests
    {
        private const ulong DumpChunkMagic = 0x1A0D234E4F4F4D1D;

        // Version 0x151: LuaNumber format (preserves integer vs float subtype)
        private const int DumpChunkVersion = 0x151;

        [global::TUnit.Core.Test]
        public async Task UndumpThrowsWhenHeaderMissing()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0UL);
            }

            stream.Position = 0;

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                processor.Undump(stream, 0, script.Globals, out bool _)
            );
            await Assert
                .That(exception.Message)
                .Contains("Not a NovaSharp chunk")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UndumpThrowsWhenVersionInvalid()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(DumpChunkMagic);
                writer.Write(DumpChunkVersion - 1);
            }

            stream.Position = 0;

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                processor.Undump(stream, 0, script.Globals, out bool _)
            );
            await Assert.That(exception.Message).Contains("Invalid version").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpThrowsWhenMetaInstructionMissing()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return 1");
            Processor processor = script.GetMainProcessorForTests();
            ByteCode byteCode = script.GetByteCodeForTests();

            int entry = chunk.Function.EntryPointByteCodeLocation;
            int invalidBase = entry;

            while (
                invalidBase < byteCode.Code.Count
                && (
                    byteCode.Code[invalidBase].OpCode == OpCode.Meta
                    || byteCode.Code[invalidBase].OpCode == OpCode.Nop
                )
            )
            {
                invalidBase++;
            }

            await Assert.That(invalidBase).IsLessThan(byteCode.Code.Count).ConfigureAwait(false);

            using MemoryStream stream = new();
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                processor.Dump(stream, invalidBase, hasUpValues: false)
            );
            await Assert.That(exception.Message).Contains("baseAddress").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpLoadRoundTripPreservesIntegerSubtype()
        {
            // Test that integer subtype is preserved through dump/load cycle
            Script script = new();
            DynValue chunk = script.LoadString("return 9007199254740993"); // 2^53 + 1, beyond double precision
            DynValue result1 = script.Call(chunk);

            // Verify the original result is an integer
            await Assert.That(result1.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result1.IsInteger).IsTrue().ConfigureAwait(false);

            // Dump and reload the bytecode
            using MemoryStream stream = new();
            script.Dump(chunk, stream);

            stream.Position = 0;
            Script script2 = new();
            DynValue loadedChunk = script2.LoadStream(stream);
            DynValue result2 = script2.Call(loadedChunk);

            // Verify the loaded result is still an integer with same value
            await Assert.That(result2.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result2.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result2.LuaNumber.AsInteger)
                .IsEqualTo(9007199254740993L)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpLoadRoundTripPreservesFloatSubtype()
        {
            // Test that float subtype is preserved through dump/load cycle
            Script script = new();
            DynValue chunk = script.LoadString("return 3.14159");
            DynValue result1 = script.Call(chunk);

            // Verify the original result is a float
            await Assert.That(result1.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result1.IsFloat).IsTrue().ConfigureAwait(false);

            // Dump and reload the bytecode
            using MemoryStream stream = new();
            script.Dump(chunk, stream);

            stream.Position = 0;
            Script script2 = new();
            DynValue loadedChunk = script2.LoadStream(stream);
            DynValue result2 = script2.Call(loadedChunk);

            // Verify the loaded result is still a float with same value
            await Assert.That(result2.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result2.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert.That(result2.LuaNumber.AsFloat).IsEqualTo(3.14159).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpLoadRoundTripPreservesNegativeZeroAsFloat()
        {
            // Negative zero must remain a float to preserve IEEE 754 semantics
            Script script = new();
            DynValue chunk = script.LoadString("return -0.0");
            DynValue result1 = script.Call(chunk);

            // Verify the original result is a float
            await Assert.That(result1.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result1.IsFloat).IsTrue().ConfigureAwait(false);

            // Dump and reload the bytecode
            using MemoryStream stream = new();
            script.Dump(chunk, stream);

            stream.Position = 0;
            Script script2 = new();
            DynValue loadedChunk = script2.LoadStream(stream);
            DynValue result2 = script2.Call(loadedChunk);

            // Verify the loaded result is still a float (negative zero must remain float)
            await Assert.That(result2.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result2.IsFloat).IsTrue().ConfigureAwait(false);
            await Assert
                .That(double.IsNegative(result2.LuaNumber.AsFloat))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DumpLoadRoundTripPreservesLargeIntegerPrecision()
        {
            // Test large integers near long.MaxValue
            Script script = new();
            // math.maxinteger = 2^63 - 1 = 9223372036854775807
            DynValue chunk = script.LoadString("return 9223372036854775807");
            DynValue result1 = script.Call(chunk);

            // Verify the original result
            await Assert.That(result1.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result1.IsInteger).IsTrue().ConfigureAwait(false);

            // Dump and reload the bytecode
            using MemoryStream stream = new();
            script.Dump(chunk, stream);

            stream.Position = 0;
            Script script2 = new();
            DynValue loadedChunk = script2.LoadStream(stream);
            DynValue result2 = script2.Call(loadedChunk);

            // Verify exact integer preservation (would lose precision if stored as double)
            await Assert.That(result2.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result2.IsInteger).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result2.LuaNumber.AsInteger)
                .IsEqualTo(long.MaxValue)
                .ConfigureAwait(false);
        }
    }
}
