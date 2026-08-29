-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:797
-- @test: DebugModuleTUnitTests.SetUserValueLua54WithNParameterSlot1Works
-- Uses injected variable: ud
local ret = debug.setuservalue(ud, 'value', "1")
                local val, hasVal = debug.getuservalue(ud, 1)
                local fractionOk, fractionError = pcall(
                    debug.setuservalue,
                    ud,
                    'mutated',
                    1.5
                )
                local infinityOk, infinityError = pcall(
                    debug.setuservalue,
                    ud,
                    'mutated',
                    math.huge
                )
                local nanOk, nanError = pcall(
                    debug.setuservalue,
                    ud,
                    'mutated',
                    0 / 0
                )
                local after = debug.getuservalue(ud, 1)
                local thousandsOk, thousandsError = pcall(
                    debug.setuservalue,
                    ud,
                    'mutated',
                    "1,000"
                )
                return ret == ud, val, hasVal,
                    fractionOk, fractionError,
                    infinityOk, infinityError,
                    nanOk, nanError,
                    after,
                    thousandsOk, thousandsError
