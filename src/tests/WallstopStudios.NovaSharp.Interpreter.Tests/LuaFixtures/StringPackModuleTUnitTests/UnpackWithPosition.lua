-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:265
-- @test: StringPackModuleTUnitTests.UnpackWithPosition
local data = string.char(10, 20, 30)
                local last = string.unpack('B', data, -1)
                local first = string.unpack('B', data, -3)
                local numericString = string.unpack('B', data, "2")
                local hexString = string.unpack('B', data, "0x2")
                local signedHexString = string.unpack('B', data, "-0x1")
                local hexFloatString = string.unpack('B', data, "0x1p0")
                local fullMaskString = string.unpack('B', data, "0xffffffffffffffff")
                local negativeFullMaskString = string.unpack(
                    'B',
                    data,
                    "-0xffffffffffffffff"
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
                    "1,000"
                )
                local endOk, endError = pcall(string.unpack, 'B', data, 4)
                local beyondOk, beyondError = pcall(string.unpack, 'B', data, 5)
                local preciseOk, preciseError = pcall(
                    string.unpack,
                    'B',
                    data,
                    "9007199254740993"
                )
                local maxIntegerOk, maxIntegerError = pcall(
                    string.unpack,
                    'B',
                    data,
                    "9223372036854775807"
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
