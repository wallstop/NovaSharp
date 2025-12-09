-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:645
-- @test: DebugModuleTUnitTests.GetLocalReturnsNilForInvalidIndexInClrFrame
return probe()
