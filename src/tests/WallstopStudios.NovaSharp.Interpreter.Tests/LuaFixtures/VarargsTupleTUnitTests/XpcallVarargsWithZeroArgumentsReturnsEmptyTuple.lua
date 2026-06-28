-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\VarargsTupleTUnitTests.cs:153
-- @test: VarargsTupleTUnitTests.XpcallVarargsWithZeroArgumentsReturnsEmptyTuple
function f(...)
                    return select('#', ...)
                end
                local ok, count = xpcall(f, function(err) end)
                return ok, count
