-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:43
-- @test: StringFormatFiniteTUnitTests.FormatUsesTwoDigitMinimumExponent
-- Compatibility notes: Test targets Lua 5.3+
return string.format('%e', 1.5) .. ',' .. string.format('%E', 0.015625)
