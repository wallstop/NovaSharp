-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:21
-- @test: JsonModuleTUnitTests.EncodeProducesCanonicalObject
-- @compat-notes: Lua 5.3+: bitwise operators
value = {
                    answer = 42,
                    enabled = true,
                    items = { 1, 2, 3 }
                }
                jsonString = json.encode(value)
