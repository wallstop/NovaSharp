-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:59
-- @test: StringFormatFiniteTUnitTests.FormatRejectsRepeatedFlagsBeforeLua54
-- Compatibility notes: Test targets Lua 5.3+
return string.format('%------9g', 1.5)
