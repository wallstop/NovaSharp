-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:353
-- @test: DebugModuleTapParityTUnitTests.TracebackIncludesMessage
return debug.traceback('traceback message')
