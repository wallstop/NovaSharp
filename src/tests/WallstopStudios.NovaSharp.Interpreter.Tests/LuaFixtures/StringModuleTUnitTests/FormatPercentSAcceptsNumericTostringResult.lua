-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:861
-- @test: StringModuleTUnitTests.FormatPercentSAcceptsNumericTostringResult
-- Compatibility notes: Test targets Lua 5.2+; Uses injected variable: s
local value = setmetatable({}, { __tostring = function() return 42 end })
                local formatted = string.format('[%s]', value)
                print(formatted)
                return formatted
