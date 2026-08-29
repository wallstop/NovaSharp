-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:387
-- @test: BasicModuleTUnitTests.WarnValidatesEveryArgumentWhileDisabled
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: warn function
warn()
