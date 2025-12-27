-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/ScriptConstructorConsistencyTUnitTests.cs:472
-- @test: ScriptConstructorConsistencyTUnitTests.StartTimeUtcIsCapturedAtConstruction
-- @compat-notes: Test targets Lua 5.1
return os.clock()
