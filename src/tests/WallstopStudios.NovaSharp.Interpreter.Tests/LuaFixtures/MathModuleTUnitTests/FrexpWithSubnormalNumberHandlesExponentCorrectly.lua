-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:257
-- @test: MathModuleTUnitTests.FrexpWithSubnormalNumberHandlesExponentCorrectly
-- This test requires C# setup (script.Globals["subnormal"] = double.Epsilon)
-- which cannot be represented in standalone Lua. Mark as NovaSharp-only.
local subnormal = 4.94065645841247e-324  -- double.Epsilon approximation
return math.frexp(subnormal)
