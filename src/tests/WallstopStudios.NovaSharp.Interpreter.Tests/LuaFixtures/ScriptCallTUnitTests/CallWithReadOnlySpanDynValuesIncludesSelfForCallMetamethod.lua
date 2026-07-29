-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:2668
-- @test: ScriptCallTUnitTests.CallWithReadOnlySpanDynValuesIncludesSelfForCallMetamethod
-- Compatibility notes: Test targets Lua 5.3+
local mt = {}
                function mt:__call(a, b, c, d, e)
                    return self.marker + a + b + c + d + e
                end

                callable = setmetatable({ marker = 100 }, mt)
