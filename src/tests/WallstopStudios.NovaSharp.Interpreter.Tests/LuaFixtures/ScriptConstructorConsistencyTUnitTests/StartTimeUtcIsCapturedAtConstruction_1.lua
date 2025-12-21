-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/ScriptConstructorConsistencyTUnitTests.cs:478
-- @test: ScriptConstructorConsistencyTUnitTests.StartTimeUtcIsCapturedAtConstruction
-- @compat-notes: Test targets Lua 5.4+
return os.clock()
