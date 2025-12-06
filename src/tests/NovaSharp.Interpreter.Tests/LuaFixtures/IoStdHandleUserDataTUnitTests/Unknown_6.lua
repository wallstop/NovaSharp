-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:101
-- @test: IoStdHandleUserDataTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
return io.output() == io.stdout
