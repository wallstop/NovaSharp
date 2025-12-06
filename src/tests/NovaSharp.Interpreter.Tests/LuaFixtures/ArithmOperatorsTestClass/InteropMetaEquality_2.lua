-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:323
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Lua 5.3+: bitwise operators
return o1 == o3
