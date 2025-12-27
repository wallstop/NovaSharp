-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2072
-- @test: StringModuleTUnitTests.FormatSWithNumbersLua52PlusDataDriven
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.2+
return string.format('%s', {luaNumber})
