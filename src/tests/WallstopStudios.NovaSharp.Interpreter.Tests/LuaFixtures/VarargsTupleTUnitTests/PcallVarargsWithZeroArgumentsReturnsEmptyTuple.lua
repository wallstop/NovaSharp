-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\VarargsTupleTUnitTests.cs:120
-- @test: VarargsTupleTUnitTests.PcallVarargsWithZeroArgumentsReturnsEmptyTuple
function f(...)
                    return select('#', ...)
                end
                local ok, count = pcall(f)
                return ok, count
