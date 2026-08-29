-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:303
-- @test: BasicModuleTUnitTests.WarnUsesConfiguredStderrWhenHandlerMissing
-- Test targets Lua 5.4+.
warn('@on'); warn('stream-', 8)
