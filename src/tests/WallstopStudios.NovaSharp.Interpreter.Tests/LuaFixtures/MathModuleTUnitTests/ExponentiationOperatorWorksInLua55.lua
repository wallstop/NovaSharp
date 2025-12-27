-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:209
-- @test: MathModuleTUnitTests.ExponentiationOperatorWorksInLua55
-- @compat-notes: Test targets Lua 5.5+
return 10 ^ 6
