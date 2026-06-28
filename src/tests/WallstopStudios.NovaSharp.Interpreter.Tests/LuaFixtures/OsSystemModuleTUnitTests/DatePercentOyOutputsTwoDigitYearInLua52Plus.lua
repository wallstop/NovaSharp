-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsSystemModuleTUnitTests.cs:508
-- @test: OsSystemModuleTUnitTests.DatePercentOyOutputsTwoDigitYearInLua52Plus
-- Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.date('!%Oy', 0)
