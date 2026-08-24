-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1662
-- @test: BasicModuleTUnitTests.RegisteredBasicCallbacksUseArgumentViews
-- Compatibility notes: Test targets Lua 5.1; Lua 5.4+: warn function
warn('caution', 9)
