-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:936
-- @test: StringModuleTUnitTests.CharHandlesPositiveInfinityAsZeroLua51And52
-- @compat-notes: Platform-specific: macOS Lua rejects infinity in string.char() while Linux/Ubuntu Lua accepts it and returns empty string. NovaSharp matches Linux behavior
return string.char(1 / 0)