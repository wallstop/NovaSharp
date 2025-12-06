-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:58
-- @test: ScriptCallTUnitTests.CallInvokesMetamethodWhenValueHasCall
-- @compat-notes: Lua 5.3+: bitwise operators
local mt = {}
                function mt:__call(value)
                    return value * 2
                end
                callable = setmetatable({}, mt)
