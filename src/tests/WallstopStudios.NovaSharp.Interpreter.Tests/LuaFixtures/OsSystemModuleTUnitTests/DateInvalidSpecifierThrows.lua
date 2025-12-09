-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:282
-- @test: OsSystemModuleTUnitTests.DateInvalidSpecifierThrows
return os.date('%Ja', 0)
