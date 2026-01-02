-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1487
-- @test: StringModuleTUnitTests.FormatPercentEscape
-- @compat-notes: Test targets Lua 5.3+
return string.format('100%% complete')
