-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:185
-- @test: Utf8ModuleTUnitTests.Utf8CodesThrowsOnInvalidUtf8Sequences
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: utf8 library
for _ in utf8.codes(invalid) do end
