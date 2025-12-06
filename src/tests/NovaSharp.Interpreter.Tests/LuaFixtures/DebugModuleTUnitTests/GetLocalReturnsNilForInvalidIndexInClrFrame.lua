-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:632
-- @test: DebugModuleTUnitTests.GetLocalReturnsNilForInvalidIndexInClrFrame
return probe()
