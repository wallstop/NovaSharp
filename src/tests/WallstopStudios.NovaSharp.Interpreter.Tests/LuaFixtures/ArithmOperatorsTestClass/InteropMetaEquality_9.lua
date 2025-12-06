-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:351
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: o1
return o1 != 'xx'
