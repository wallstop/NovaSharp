-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:580
-- @test: StringModuleTUnitTests.SubErrorsOnNonIntegerIndexLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.sub('Lua', 1.5, 3)
