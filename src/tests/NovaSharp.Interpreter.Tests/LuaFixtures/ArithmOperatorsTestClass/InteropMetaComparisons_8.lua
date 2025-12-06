-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:403
-- @test: ArithmOperatorsTestClass.InteropMetaComparisons
-- @compat-notes: Uses injected variable: o1
return 4 > o1
