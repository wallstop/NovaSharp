namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using global::TUnit.Assertions.Extensions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for Lua 5.3+ string.pack, string.unpack, and string.packsize functions.
    /// These functions provide binary serialization/deserialization capabilities.
    /// </summary>
    public sealed class StringPackModuleTUnitTests : LuaSpecTestBase
    {
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackInteger(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('i4', 42)
                local unpacked = string.unpack('i4', packed)
                return unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(42);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackNegativeInteger(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('i4', -12345)
                local unpacked = string.unpack('i4', packed)
                return unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(-12345);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackSignedByte(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('b', -1)
                local unpacked = string.unpack('b', packed)
                return unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(-1);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackUnsignedByte(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('B', 255)
                local unpacked = string.unpack('B', packed)
                return unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(255);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackDouble(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('d', 3.14159265358979)
                local unpacked = string.unpack('d', packed)
                return unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(3.14159265358979);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackFloat(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('f', 3.14)
                local unpacked = string.unpack('f', packed)
                return math.abs(unpacked - 3.14) < 0.001
                "
            );

            await Assert.That(result.Boolean).IsTrue();
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackZeroTerminatedString(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('z', 'hello')
                local unpacked = string.unpack('z', packed)
                return unpacked
                "
            );

            await Assert.That(result.String).IsEqualTo("hello");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackLengthPrefixedString(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('s4', 'world')
                local unpacked = string.unpack('s4', packed)
                return unpacked
                "
            );

            await Assert.That(result.String).IsEqualTo("world");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackFixedSizeString(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('c10', 'test')
                local unpacked = string.unpack('c10', packed)
                return #unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(10);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackLittleEndian(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('<I2', 0x0102)
                local b1 = string.byte(packed, 1)
                local b2 = string.byte(packed, 2)
                return b1, b2
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0x02);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0x01);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackBigEndian(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('>I2', 0x0102)
                local b1 = string.byte(packed, 1)
                local b2 = string.byte(packed, 2)
                return b1, b2
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0x01);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0x02);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackUnpackMultipleValues(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('i4 i4 z', 100, 200, 'test')
                local a, b, c = string.unpack('i4 i4 z', packed)
                return a, b, c
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(100);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(200);
            await Assert.That(result.Tuple[2].String).IsEqualTo("test");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackSizeReturnsCorrectSize(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString("return string.packsize('i4 d B')");

            await Assert.That(result.Number).IsEqualTo(4 + 8 + 1);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackSizeThrowsOnVariableLength(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.packsize('z')")
            );

            await Assert.That(exception.Message).Contains("variable-length");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task UnpackReturnsNextPosition(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('i4 i4', 10, 20)
                local a, b, nextpos = string.unpack('i4 i4', packed)
                return nextpos
                "
            );

            await Assert.That(result.Number).IsEqualTo(9);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task UnpackWithPosition(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local data = string.char(10, 20, 30)
                local last = string.unpack('B', data, -1)
                local first = string.unpack('B', data, -3)
                local numericString = string.unpack('B', data, ""2"")
                local hexString = string.unpack('B', data, ""0x2"")
                local signedHexString = string.unpack('B', data, ""-0x1"")
                local hexFloatString = string.unpack('B', data, ""0x1p0"")
                local fullMaskString = string.unpack('B', data, ""0xffffffffffffffff"")
                local negativeFullMaskString = string.unpack(
                    'B',
                    data,
                    ""-0xffffffffffffffff""
                )
                local integralFloat = string.unpack('B', data, 1.0)
                local zeroOk, zeroResult = pcall(string.unpack, 'B', data, 0)
                local beforeOk, beforeResult = pcall(string.unpack, 'B', data, -4)
                local fractionOk, fractionError = pcall(string.unpack, 'B', data, 1.5)
                local infinityOk, infinityError = pcall(string.unpack, 'B', data, math.huge)
                local nanOk, nanError = pcall(string.unpack, 'B', data, 0 / 0)
                local thousandsOk, thousandsError = pcall(
                    string.unpack,
                    'B',
                    data,
                    ""1,000""
                )
                local endOk, endError = pcall(string.unpack, 'B', data, 4)
                local beyondOk, beyondError = pcall(string.unpack, 'B', data, 5)
                local preciseOk, preciseError = pcall(
                    string.unpack,
                    'B',
                    data,
                    ""9007199254740993""
                )
                local maxIntegerOk, maxIntegerError = pcall(
                    string.unpack,
                    'B',
                    data,
                    ""9223372036854775807""
                )
                print(
                    'relative',
                    last,
                    first,
                    numericString,
                    hexString,
                    signedHexString,
                    hexFloatString,
                    fullMaskString,
                    negativeFullMaskString,
                    integralFloat
                )
                print(
                    'zero-before',
                    zeroOk,
                    zeroOk and zeroResult or string.find(zeroResult, 'initial position') ~= nil,
                    beforeOk,
                    beforeOk and beforeResult or string.find(beforeResult, 'initial position') ~= nil
                )
                print(
                    'integer-errors',
                    fractionOk,
                    string.find(fractionError, 'integer representation') ~= nil,
                    infinityOk,
                    string.find(infinityError, 'integer representation') ~= nil,
                    nanOk,
                    string.find(nanError, 'integer representation') ~= nil,
                    thousandsOk,
                    string.find(thousandsError, 'number expected') ~= nil
                )
                print(
                    'bounds',
                    endOk,
                    string.find(endError, 'data string too short') ~= nil,
                    beyondOk,
                    string.find(beyondError, 'initial position') ~= nil
                )
                print(
                    'precise-bounds',
                    preciseOk,
                    string.find(preciseError, 'initial position') ~= nil,
                    maxIntegerOk,
                    string.find(maxIntegerError, 'initial position') ~= nil
                )
                return last, first, numericString, hexString,
                    signedHexString, hexFloatString,
                    fullMaskString, negativeFullMaskString, integralFloat,
                    zeroOk, zeroResult,
                    beforeOk, beforeResult,
                    fractionOk, fractionError,
                    infinityOk, infinityError,
                    nanOk, nanError,
                    thousandsOk, thousandsError,
                    endOk, endError,
                    beyondOk, beyondError,
                    preciseOk, preciseError,
                    maxIntegerOk, maxIntegerError
                "
            );

            LuaValue[] tuple = result.Tuple ?? System.Array.Empty<LuaValue>();
            await Assert.That(tuple[0].Number).IsEqualTo(30);
            await Assert.That(tuple[1].Number).IsEqualTo(10);
            await Assert.That(tuple[2].Number).IsEqualTo(20);
            await Assert.That(tuple[3].Number).IsEqualTo(20);
            await Assert.That(tuple[4].Number).IsEqualTo(30);
            await Assert.That(tuple[5].Number).IsEqualTo(10);
            await Assert.That(tuple[6].Number).IsEqualTo(30);
            await Assert.That(tuple[7].Number).IsEqualTo(10);
            await Assert.That(tuple[8].Number).IsEqualTo(10);

            bool clampsToFirst = version >= LuaCompatibilityVersion.Lua54;
            await Assert.That(tuple[9].CastToBool()).IsEqualTo(clampsToFirst);
            await Assert.That(tuple[11].CastToBool()).IsEqualTo(clampsToFirst);
            if (clampsToFirst)
            {
                await Assert.That(tuple[10].Number).IsEqualTo(10);
                await Assert.That(tuple[12].Number).IsEqualTo(10);
            }
            else
            {
                await Assert.That(tuple[10].String).Contains("initial position out of string");
                await Assert.That(tuple[12].String).Contains("initial position out of string");
            }

            for (int index = 13; index <= 17; index += 2)
            {
                await Assert.That(tuple[index].CastToBool()).IsFalse();
                await Assert.That(tuple[index + 1].String).Contains("integer representation");
            }

            await Assert.That(tuple[19].CastToBool()).IsFalse();
            await Assert.That(tuple[20].String).Contains("number expected");
            await Assert.That(tuple[21].CastToBool()).IsFalse();
            await Assert.That(tuple[22].String).Contains("data string too short");
            await Assert.That(tuple[23].CastToBool()).IsFalse();
            await Assert.That(tuple[24].String).Contains("initial position out of string");
            await Assert.That(tuple[25].CastToBool()).IsFalse();
            await Assert.That(tuple[26].String).Contains("initial position out of string");
            await Assert.That(tuple[27].CastToBool()).IsFalse();
            await Assert.That(tuple[28].String).Contains("initial position out of string");
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task PackUnavailableInLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("string.pack('i4', 42)")
            );

            await Assert.That(exception.Message).Contains("requires Lua 5.3");
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task UnpackUnavailableInLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("string.unpack('i4', 'test')")
            );

            await Assert.That(exception.Message).Contains("requires Lua 5.3");
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task PackSizeUnavailableInLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("string.packsize('i4')")
            );

            await Assert.That(exception.Message).Contains("requires Lua 5.3");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackLuaInteger(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('j', 9223372036854775807)
                local unpacked = string.unpack('j', packed)
                return unpacked
                "
            );

            await Assert.That(result.Number).IsEqualTo(9223372036854775807);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task PackPaddingByte(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            LuaValue result = script.DoString(
                @"
                local packed = string.pack('BxB', 1, 2)
                return #packed
                "
            );

            await Assert.That(result.Number).IsEqualTo(3);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task InvalidFormatOptionThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("string.pack('Q', 42)")
            );

            await Assert.That(exception.Message).Contains("invalid format option");
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ZeroTerminatedStringWithNullThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("string.pack('z', 'hello\\0world')")
            );

            await Assert.That(exception.Message).Contains("contains zeros");
        }
    }
}
