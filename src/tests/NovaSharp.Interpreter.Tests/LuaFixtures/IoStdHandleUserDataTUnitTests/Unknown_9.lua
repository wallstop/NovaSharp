-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:130
-- @test: IoStdHandleUserDataTUnitTests.Unknown
return io.stdout[1]
