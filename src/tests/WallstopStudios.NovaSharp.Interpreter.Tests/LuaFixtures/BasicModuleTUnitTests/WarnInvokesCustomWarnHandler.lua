-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:283
-- @test: BasicModuleTUnitTests.WarnInvokesCustomWarnHandler
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: warn function
warn('@on'); warn('custom-', 7)
