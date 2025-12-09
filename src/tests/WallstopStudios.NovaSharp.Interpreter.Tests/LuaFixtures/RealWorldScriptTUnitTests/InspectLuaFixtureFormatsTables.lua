-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/RealWorldScriptTUnitTests.cs:69
-- @test: RealWorldScriptTUnitTests.InspectLuaFixtureFormatsTables
-- @compat-notes: Lua 5.3+: bitwise operators
return {
                        answer = 42,
                        nested = { 1, 2, 3 },
                        label = 'NovaSharp',
                        active = true
                    }
