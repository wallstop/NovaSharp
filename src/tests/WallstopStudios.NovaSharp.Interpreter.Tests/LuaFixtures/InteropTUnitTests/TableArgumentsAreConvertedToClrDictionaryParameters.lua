-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\InteropTUnitTests.cs:56
-- @test: InteropTUnitTests.TableArgumentsAreConvertedToClrDictionaryParameters
-- @compat-notes: Lua 5.3+: bitwise operators
return sum({ x = 10, y = 32 })
