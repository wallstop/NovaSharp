-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:353
-- @test: DebugModuleTapParityTUnitTests.Unknown
return debug.traceback('traceback message')
