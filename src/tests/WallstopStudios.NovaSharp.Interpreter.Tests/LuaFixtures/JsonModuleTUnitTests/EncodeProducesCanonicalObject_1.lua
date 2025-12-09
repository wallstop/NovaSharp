-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:21
-- @test: JsonModuleTUnitTests.EncodeProducesCanonicalObject
-- @compat-notes: Test class 'JsonModuleTUnitTests' uses NovaSharp-specific JsonModule functionality
value = {
                    answer = 42,
                    enabled = true,
                    items = { 1, 2, 3 }
                }
                jsonString = json.encode(value)
