-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:505
-- @test: IoModuleTUnitTests.OpenThrowsWhenModeEmpty
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return io.open('{path}', "")
