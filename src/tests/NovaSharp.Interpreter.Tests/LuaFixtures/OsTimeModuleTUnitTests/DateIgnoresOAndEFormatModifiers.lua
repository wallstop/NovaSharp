-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:203
-- @test: OsTimeModuleTUnitTests.DateIgnoresOAndEFormatModifiers
return os.date('!%OY-%Ew', 0)
