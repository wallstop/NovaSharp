-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:149
-- @test: MathModuleTUnitTests.TypeDistinguishesIntegersAndFloats
-- @compat-notes: Lua 5.3+: math.type (5.3+)
return math.type(5), math.type(3.14)
