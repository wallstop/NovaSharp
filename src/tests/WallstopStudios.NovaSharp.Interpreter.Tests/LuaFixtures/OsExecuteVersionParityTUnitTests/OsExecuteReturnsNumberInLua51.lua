-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs:39
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsNumberInLua51
-- @compat-notes: Test requires stubbed platform; not comparable against native Lua
return os.execute('build')
