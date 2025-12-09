-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataMetaTUnitTests.cs:315
-- @test: ArithmOperatorsTestClass.InteropMetaOpLen
-- @compat-notes: Uses injected variable: o2
return #o2
