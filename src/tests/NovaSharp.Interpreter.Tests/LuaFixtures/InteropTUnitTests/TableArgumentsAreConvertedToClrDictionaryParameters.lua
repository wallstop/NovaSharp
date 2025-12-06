-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/InteropTUnitTests.cs:56
-- @test: InteropTUnitTests.TableArgumentsAreConvertedToClrDictionaryParameters
-- @compat-notes: Lua 5.3+: bitwise operators
return sum({ x = 10, y = 32 })
