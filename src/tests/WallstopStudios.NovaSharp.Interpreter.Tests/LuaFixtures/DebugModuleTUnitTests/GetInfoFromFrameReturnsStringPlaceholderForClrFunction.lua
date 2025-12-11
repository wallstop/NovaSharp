-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:568
-- @test: DebugModuleTUnitTests.GetInfoFromFrameReturnsStringPlaceholderForClrFunction
-- @compat-notes: Uses injected variable: callback
return callback()
