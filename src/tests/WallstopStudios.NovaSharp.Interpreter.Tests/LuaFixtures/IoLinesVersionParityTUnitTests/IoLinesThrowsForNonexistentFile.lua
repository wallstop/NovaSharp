-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:229
-- @test: IoLinesVersionParityTUnitTests.IoLinesThrowsForNonexistentFile
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
for line in io.lines('{path}') do end
