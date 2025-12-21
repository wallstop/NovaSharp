-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:249
-- @test: IoLinesVersionParityTUnitTests.IoLinesThrowsForNonexistentFile
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
for line in io.lines('{path}') do end
