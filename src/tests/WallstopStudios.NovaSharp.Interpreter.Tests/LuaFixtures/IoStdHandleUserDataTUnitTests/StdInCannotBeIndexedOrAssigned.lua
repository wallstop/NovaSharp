-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoStdHandleUserDataTUnitTests.cs:121
-- @test: IoStdHandleUserDataTUnitTests.StdInCannotBeIndexedOrAssigned
return io.stdin[1]
