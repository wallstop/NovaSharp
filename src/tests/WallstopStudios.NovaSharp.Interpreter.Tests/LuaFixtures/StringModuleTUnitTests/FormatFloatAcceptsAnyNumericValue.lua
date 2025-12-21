-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2341
-- @test: StringModuleTUnitTests.FormatFloatAcceptsAnyNumericValue
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.4+
return string.format('%f', {luaValue})
