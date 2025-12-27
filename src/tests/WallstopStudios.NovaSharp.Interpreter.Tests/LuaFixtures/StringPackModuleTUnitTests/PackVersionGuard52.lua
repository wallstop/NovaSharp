-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackVersionGuard52

-- Test: string.pack is unavailable in Lua 5.2 (should error)

string.pack("i4", 42)
