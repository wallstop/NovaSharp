-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:126
-- @test: IoStdHandleUserDataTUnitTests.StdInCannotBeIndexedOrAssigned
io.stdin[1] = 42
