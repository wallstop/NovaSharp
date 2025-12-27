-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackSizeBasic

-- Test: packsize function returns correct sizes for format strings

-- Size of i4 (4-byte integer)
print(string.packsize("i4"))

-- Size of double
print(string.packsize("d"))

-- Size of byte + double
print(string.packsize("b d"))
