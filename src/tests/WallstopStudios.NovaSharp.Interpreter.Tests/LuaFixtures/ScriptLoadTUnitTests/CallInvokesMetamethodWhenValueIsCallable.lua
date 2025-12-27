-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptLoadTUnitTests.cs:347
-- @test: ScriptLoadTUnitTests.CallInvokesMetamethodWhenValueIsCallable
-- @compat-notes: Test targets Lua 5.1
local t = {}
                setmetatable(t, { __call = function(_, value) return value * 2 end })
                return t
