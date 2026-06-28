-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaMathMultiVersionSpecTUnitTests.cs:131
-- @test: LuaMathMultiVersionSpecTUnitTests.MathUltRejectsNonIntegerArgumentsAcrossLua53PlusVersions
-- Test targets Lua 5.3+; Lua 5.3+: bitwise operators
local ok, err = pcall(math.ult, 1.5, 2) return ok, err
