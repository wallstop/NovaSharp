-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/VarargsTupleTUnitTests.cs:93
-- @test: VarargsTupleTUnitTests.VarargsWithZeroArgumentsReturnsEmptyTuple
function f(...)
                    return select('#', ...)
                end
                return f()  -- Should be 0, not 1
