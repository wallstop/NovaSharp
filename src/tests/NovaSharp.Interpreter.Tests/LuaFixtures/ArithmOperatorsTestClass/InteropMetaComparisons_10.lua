-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:412
-- @test: ArithmOperatorsTestClass.InteropMetaComparisons
-- @compat-notes: Uses injected variable: o1
return o1 > o2
