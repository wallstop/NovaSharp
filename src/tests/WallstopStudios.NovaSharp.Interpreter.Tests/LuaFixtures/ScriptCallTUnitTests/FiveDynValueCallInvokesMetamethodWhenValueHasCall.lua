-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:337
-- @test: ScriptCallTUnitTests.FiveDynValueCallInvokesMetamethodWhenValueHasCall
-- Compatibility notes: Test targets Lua 5.5+
local mt = {}
                function mt:__call(a, b, c, d, e)
                    return self.marker + a + b + c + d + e
                end
                callable = setmetatable({ marker = 100 }, mt)
