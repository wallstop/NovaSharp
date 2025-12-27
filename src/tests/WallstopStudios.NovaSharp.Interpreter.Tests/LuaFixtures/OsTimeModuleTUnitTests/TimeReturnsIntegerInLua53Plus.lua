-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:566
-- @test: OsTimeModuleTUnitTests.TimeReturnsIntegerInLua53Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: math.type (5.3+)
return math.type(os.time({year=2000, month=1, day=1, hour=0, min=0, sec=0}))
