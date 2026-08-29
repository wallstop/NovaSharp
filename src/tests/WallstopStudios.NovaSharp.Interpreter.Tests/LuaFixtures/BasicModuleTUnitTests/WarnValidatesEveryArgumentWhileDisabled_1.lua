-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:390
-- @test: BasicModuleTUnitTests.WarnValidatesEveryArgumentWhileDisabled
-- Test targets Lua 5.4+.
warn('valid', true)
