-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptLoadTUnitTests.cs:255
-- @test: ScriptLoadTUnitTests.CallInvokesMetamethodWhenValueIsCallable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                setmetatable(t, { __call = function(_, value) return value * 2 end })
                return t
