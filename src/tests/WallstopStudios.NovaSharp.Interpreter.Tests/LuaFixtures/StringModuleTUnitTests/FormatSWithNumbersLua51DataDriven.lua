-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2038
-- @test: StringModuleTUnitTests.FormatSWithNumbersLua51DataDriven
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
return string.format('%s', {luaNumber})
