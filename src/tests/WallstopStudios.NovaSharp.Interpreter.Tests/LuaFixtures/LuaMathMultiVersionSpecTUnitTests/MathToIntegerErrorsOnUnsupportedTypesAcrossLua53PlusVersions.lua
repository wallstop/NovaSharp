-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:76
-- @test: LuaMathMultiVersionSpecTUnitTests.MathToIntegerErrorsOnUnsupportedTypesAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise operators
local ok, err = pcall(math.tointeger, {}) return ok, err
