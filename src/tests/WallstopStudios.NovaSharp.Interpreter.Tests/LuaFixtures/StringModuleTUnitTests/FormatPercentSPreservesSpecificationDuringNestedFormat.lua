-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:917
-- @test: StringModuleTUnitTests.FormatPercentSPreservesSpecificationDuringNestedFormat
-- Compatibility notes: Test targets Lua 5.2+; Uses injected variable: s
local value = setmetatable({}, {
                    __tostring = function() return string.format('%s', 'x') end
                })
                local formatted = string.format('[%10s]', value)
                print(formatted)
                return formatted
