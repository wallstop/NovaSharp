-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptLoadTUnitTests.cs:255
-- @test: ScriptLoadTUnitTests.CallInvokesMetamethodWhenValueIsCallable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                setmetatable(t, { __call = function(_, value) return value * 2 end })
                return t
