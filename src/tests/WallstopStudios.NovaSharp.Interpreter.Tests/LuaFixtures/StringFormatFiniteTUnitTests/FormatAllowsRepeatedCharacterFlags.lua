-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:169
-- @test: StringFormatFiniteTUnitTests.FormatAllowsRepeatedCharacterFlags
-- Compatibility notes: Test targets Lua 5.4+
local outcome = string.format('%------9c', 65)
                print(outcome)
                return outcome
