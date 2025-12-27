-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1096
-- @test: DebugModuleTUnitTests.GetInfoFromFrameReturnsStringPlaceholderForClrFunction
-- @compat-notes: Test targets Lua 5.4+; Uses injected variable: callback
return callback()
