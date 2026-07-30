-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:1345
-- @test: ErrorHandlingModuleTUnitTests.XpcallIgnoresAllExtraArgumentsInLua51
-- Compatibility notes: Test targets Lua 5.4+
return function(err) return err end
