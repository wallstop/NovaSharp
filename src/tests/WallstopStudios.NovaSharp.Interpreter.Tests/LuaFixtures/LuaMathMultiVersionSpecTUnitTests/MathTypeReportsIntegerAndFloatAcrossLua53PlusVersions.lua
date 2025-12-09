-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:42
-- @test: LuaMathMultiVersionSpecTUnitTests.MathTypeReportsIntegerAndFloatAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: math.type (5.3+)
return math.type(5), math.type(3.5), math.type(1.0)
