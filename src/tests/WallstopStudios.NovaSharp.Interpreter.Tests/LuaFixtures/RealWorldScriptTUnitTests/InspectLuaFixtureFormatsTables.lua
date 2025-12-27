-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/RealWorldScriptTUnitTests.cs:79
-- @test: RealWorldScriptTUnitTests.InspectLuaFixtureFormatsTables
return {
                        answer = 42,
                        nested = { 1, 2, 3 },
                        label = 'NovaSharp',
                        active = true
                    }
