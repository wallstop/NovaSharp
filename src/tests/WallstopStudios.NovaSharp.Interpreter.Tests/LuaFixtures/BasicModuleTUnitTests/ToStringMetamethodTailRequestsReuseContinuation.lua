-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:42
-- @test: BasicModuleTUnitTests.ToStringMetamethodTailRequestsReuseContinuation
-- Compatibility notes: Test targets Lua 5.2+
return setmetatable({}, { __tostring = function() return 'value' end })
