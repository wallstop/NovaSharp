-- @lua-versions: 5.1-5.2
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:936
-- @test: StringModuleTUnitTests.CharHandlesPositiveInfinityAsZeroLua51And52
-- string.char(inf) behavior varies by platform; macOS Lua 5.1 throws error while other platforms return null byte
return string.char(1/0)
