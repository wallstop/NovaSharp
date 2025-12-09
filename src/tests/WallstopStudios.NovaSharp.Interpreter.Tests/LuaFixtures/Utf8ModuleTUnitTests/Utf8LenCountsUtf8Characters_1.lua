-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:37
-- @test: Utf8ModuleTUnitTests.Utf8LenCountsUtf8Characters
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: utf8 library
return utf8.len(sample, 5, 5)
