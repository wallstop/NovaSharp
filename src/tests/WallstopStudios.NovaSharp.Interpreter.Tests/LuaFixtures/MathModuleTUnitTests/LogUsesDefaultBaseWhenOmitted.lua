-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:23
-- @test: MathModuleTUnitTests.LogUsesDefaultBaseWhenOmitted
-- @compat-notes: Test targets Lua 5.2+
return math.log(8)
