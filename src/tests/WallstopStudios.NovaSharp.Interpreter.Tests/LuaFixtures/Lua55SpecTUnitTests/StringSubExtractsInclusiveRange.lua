-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:105
-- @test: Lua55SpecTUnitTests.StringSubExtractsInclusiveRange
return string.sub('abcdefg', 2, 4)
