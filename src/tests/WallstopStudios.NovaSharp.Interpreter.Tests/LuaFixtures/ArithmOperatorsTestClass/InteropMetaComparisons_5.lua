-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:417
-- @test: ArithmOperatorsTestClass.InteropMetaComparisons
-- @compat-notes: Uses injected variable: o2
return o2 >= o2
