-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:374
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Uses injected variable: o1
return 'xx' != o1
