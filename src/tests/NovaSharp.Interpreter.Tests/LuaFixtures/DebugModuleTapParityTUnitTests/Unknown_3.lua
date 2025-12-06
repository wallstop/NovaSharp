-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:86
-- @test: DebugModuleTapParityTUnitTests.Unknown
return debug.getinfo(999)
