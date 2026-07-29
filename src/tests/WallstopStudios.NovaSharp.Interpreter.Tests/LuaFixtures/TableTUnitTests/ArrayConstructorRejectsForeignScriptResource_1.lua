-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:226
-- @test: TableTUnitTests.ArrayConstructorRejectsForeignScriptResource
-- Compatibility notes: Test targets Lua 5.4+
return { foreign() }
