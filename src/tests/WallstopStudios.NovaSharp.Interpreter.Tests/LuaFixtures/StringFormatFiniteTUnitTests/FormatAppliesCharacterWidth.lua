-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:155
-- @test: StringFormatFiniteTUnitTests.FormatAppliesCharacterWidth
-- Compatibility notes: Test targets Lua 5.4+
local outcome = '[' .. string.format('%5c', 65) .. '][' .. string.format('%-5c', 65) .. ']'
                print(outcome)
                return outcome
