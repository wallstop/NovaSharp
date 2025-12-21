-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1489
-- @test: StringModuleTUnitTests.FormatUnsignedWithFieldWidth
-- @compat-notes: Test targets Lua 5.1
return string.format('%8u', 42)
