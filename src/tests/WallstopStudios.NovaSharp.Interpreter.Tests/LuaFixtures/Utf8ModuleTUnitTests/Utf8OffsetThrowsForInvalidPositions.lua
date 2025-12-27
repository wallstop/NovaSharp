-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:759
-- @test: Utf8ModuleTUnitTests.Utf8OffsetThrowsForInvalidPositions
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.3+
return utf8.offset(s, {n}, {pos})
