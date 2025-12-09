-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:161
-- @test: OsTimeModuleTUnitTests.DateReturnsTableWhenRequested
return os.date('!*t', 1609459200)
