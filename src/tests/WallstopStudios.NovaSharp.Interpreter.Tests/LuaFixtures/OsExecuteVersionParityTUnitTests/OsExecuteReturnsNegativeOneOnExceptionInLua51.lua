-- @lua-versions: 5.1, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs:75
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsNegativeOneOnExceptionInLua51
-- @compat-notes: Test requires stubbed platform; not comparable against native Lua
return os.execute('fail')
