-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\VarargsTupleTUnitTests.cs:185
-- @test: VarargsTupleTUnitTests.VarargsDistinguishesZeroArgsFromOneNilArg
function f(...)
                    return select('#', ...)
                end
                local zeroArgs = f()
                local oneNilArg = f(nil)
                return zeroArgs, oneNilArg
