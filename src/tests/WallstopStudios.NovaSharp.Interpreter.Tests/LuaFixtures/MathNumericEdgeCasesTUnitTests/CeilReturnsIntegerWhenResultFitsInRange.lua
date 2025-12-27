-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:1235
-- @test: MathNumericEdgeCasesTUnitTests.CeilReturnsIntegerWhenResultFitsInRange
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.3+
local v = math.ceil({luaExpression})
                return math.type(v), v
