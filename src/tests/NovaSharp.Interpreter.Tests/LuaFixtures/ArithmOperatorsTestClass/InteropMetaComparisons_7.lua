-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:398
-- @test: ArithmOperatorsTestClass.InteropMetaComparisons
-- @compat-notes: Uses injected variable: o1
return o1 < 4
