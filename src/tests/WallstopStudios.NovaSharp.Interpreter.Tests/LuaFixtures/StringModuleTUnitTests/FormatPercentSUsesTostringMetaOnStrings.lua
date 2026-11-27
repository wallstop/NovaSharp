-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:878
-- @test: StringModuleTUnitTests.FormatPercentSUsesTostringMetaOnStrings
-- Compatibility notes: Test targets Lua 5.2+; Uses injected variable: s
debug.setmetatable('', { __tostring = function() return 'META' end })
                local formatted = string.format('[%s]', 'abc')
                debug.setmetatable('', nil)
                print(formatted)
                return formatted
