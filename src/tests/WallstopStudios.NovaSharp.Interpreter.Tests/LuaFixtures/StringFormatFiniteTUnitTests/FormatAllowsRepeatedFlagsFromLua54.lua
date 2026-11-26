-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:185
-- @test: StringFormatFiniteTUnitTests.FormatAllowsRepeatedFlagsFromLua54
-- Compatibility notes: Test targets Lua 5.3+
local outcome = string.format('%------9g', 1.5) print(outcome) return outcome
