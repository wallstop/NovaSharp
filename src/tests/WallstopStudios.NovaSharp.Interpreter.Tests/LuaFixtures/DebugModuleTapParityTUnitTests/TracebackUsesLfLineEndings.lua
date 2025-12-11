-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:365
-- @test: DebugModuleTapParityTUnitTests.TracebackUsesLfLineEndings
return debug.traceback()
