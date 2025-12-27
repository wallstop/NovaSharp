-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:354
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Uses injected variable: o2
return o2 != o3
