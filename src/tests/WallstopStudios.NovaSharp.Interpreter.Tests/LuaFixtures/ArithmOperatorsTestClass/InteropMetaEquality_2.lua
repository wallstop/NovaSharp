-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:350
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Uses injected variable: o3
return o1 == o3
