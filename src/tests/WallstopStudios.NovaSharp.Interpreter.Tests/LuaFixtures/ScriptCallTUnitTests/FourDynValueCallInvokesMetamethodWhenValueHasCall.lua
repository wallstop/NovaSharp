-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:283
-- @test: ScriptCallTUnitTests.FourDynValueCallInvokesMetamethodWhenValueHasCall
-- Compatibility notes: Test targets Lua 5.1
local mt = {}
                function mt:__call(a, b, c, d)
                    return a + b + c + d
                end
                callable = setmetatable({}, mt)
