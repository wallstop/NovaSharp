-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:88
-- @test: StringFormatFiniteTUnitTests.FormatRejectsCharacterPrecision
-- Compatibility notes: Test targets Lua 5.3+
return string.format('%.1c', 65)
