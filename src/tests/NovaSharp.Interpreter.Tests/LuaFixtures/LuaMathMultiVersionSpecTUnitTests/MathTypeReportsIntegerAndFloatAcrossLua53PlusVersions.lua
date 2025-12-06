-- @lua-versions: 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:42
-- @test: LuaMathMultiVersionSpecTUnitTests.MathTypeReportsIntegerAndFloatAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.2+
return math.type(5), math.type(3.5), math.type(1.0)
