-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:215
-- @test: OsTimeModuleTUnitTests.DateIgnoresOAndEFormatModifiers
return os.date('!%OY-%Ew', 0)
