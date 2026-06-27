-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:268
-- @test: Utf8ModuleTUnitTests.Utf8CodesThrowsOnInvalidUtf8Sequences
-- Test targets Lua 5.3+; Lua 5.3+: utf8 library
for _ in utf8.codes(invalid) do end
