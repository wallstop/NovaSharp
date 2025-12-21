-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:346
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Uses injected variable: o1
return o1 != o2
