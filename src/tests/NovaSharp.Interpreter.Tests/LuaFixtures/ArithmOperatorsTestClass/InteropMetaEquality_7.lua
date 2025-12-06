-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:343
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: o1
return 6 != o1
