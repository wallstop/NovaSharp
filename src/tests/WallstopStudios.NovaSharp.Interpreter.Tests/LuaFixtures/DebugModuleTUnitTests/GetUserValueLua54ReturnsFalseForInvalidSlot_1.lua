-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:664
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsFalseForInvalidSlot
-- Uses injected variable: ud
local val, hasVal = debug.getuservalue(ud, 2)
                local stringVal, stringHasVal = debug.getuservalue(ud, "1")
                local fractionOk, fractionError = pcall(debug.getuservalue, ud, 1.5)
                local infinityOk, infinityError = pcall(debug.getuservalue, ud, math.huge)
                local nanOk, nanError = pcall(debug.getuservalue, ud, 0 / 0)
                local preciseVal, preciseHasVal = debug.getuservalue(
                    ud,
                    "9007199254740993"
                )
                local maxIntegerOk, maxIntegerVal, maxIntegerHasVal = pcall(
                    debug.getuservalue,
                    ud,
                    "9223372036854775807"
                )
                local hexVal, hexHasVal = debug.getuservalue(ud, "0x100000001")
                local signedHexVal, signedHexHasVal = debug.getuservalue(
                    ud,
                    "-0xffffffff"
                )
                local hexFloatVal, hexFloatHasVal = debug.getuservalue(ud, "0x1p0")
                local fullMaskOk, fullMaskVal, fullMaskHasVal = pcall(
                    debug.getuservalue,
                    ud,
                    "0xffffffffffffffff"
                )
                local negativeFullMaskVal, negativeFullMaskHasVal = debug.getuservalue(
                    ud,
                    "-0xffffffffffffffff"
                )
                local thousandsOk, thousandsError = pcall(
                    debug.getuservalue,
                    ud,
                    "1,000"
                )
                return val, hasVal, stringVal, stringHasVal,
                    fractionOk, fractionError,
                    infinityOk, infinityError,
                    nanOk, nanError,
                    preciseVal, preciseHasVal,
                    maxIntegerOk, maxIntegerVal, maxIntegerHasVal,
                    hexVal, hexHasVal,
                    signedHexVal, signedHexHasVal,
                    hexFloatVal, hexFloatHasVal,
                    fullMaskOk, fullMaskVal, fullMaskHasVal,
                    negativeFullMaskVal, negativeFullMaskHasVal,
                    thousandsOk, thousandsError
