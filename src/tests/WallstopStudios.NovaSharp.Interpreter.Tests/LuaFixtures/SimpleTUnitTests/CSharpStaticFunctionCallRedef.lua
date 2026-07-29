-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:91
-- @test: SimpleTUnitTests.CSharpStaticFunctionCallRedef
local print = print; print("hello", "world");
