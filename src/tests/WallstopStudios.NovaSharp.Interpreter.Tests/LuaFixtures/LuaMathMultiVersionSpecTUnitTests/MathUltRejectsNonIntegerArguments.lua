-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:115
-- @test: LuaMathMultiVersionSpecTUnitTests.MathUltRejectsNonIntegerArguments
-- @compat-notes: Test targets Lua 5.3+
local ok, err = pcall(math.ult, 1.5, 2) return ok, err
