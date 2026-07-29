-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1095
-- @test: DebugModuleTUnitTests.GetInfoFromFrameReturnsClrFunctionWithFFlag
-- Compatibility notes: Test targets Lua 5.1; Uses injected variable: callback
return callback()
