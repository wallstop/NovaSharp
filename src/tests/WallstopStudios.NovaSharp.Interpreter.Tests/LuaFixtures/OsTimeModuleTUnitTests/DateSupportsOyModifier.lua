-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:213
-- @test: OsTimeModuleTUnitTests.DateSupportsOyModifier
return os.date('!%Oy', 0)
