-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:98
-- @test: DebugModuleTapParityTUnitTests.Unknown
debug.getinfo('invalid')
