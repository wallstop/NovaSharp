-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:80
-- @test: ScriptCallTUnitTests.DynValueCallInvokesZeroArgumentLuaFunction
-- Compatibility notes: Test targets Lua 5.2+
return function() return 42 end
