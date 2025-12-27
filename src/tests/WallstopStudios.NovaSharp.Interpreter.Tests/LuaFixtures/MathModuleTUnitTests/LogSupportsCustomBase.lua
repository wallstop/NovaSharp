-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:37
-- @test: MathModuleTUnitTests.LogSupportsCustomBase
-- @compat-notes: Test targets Lua 5.1
return math.log(8, 2)
