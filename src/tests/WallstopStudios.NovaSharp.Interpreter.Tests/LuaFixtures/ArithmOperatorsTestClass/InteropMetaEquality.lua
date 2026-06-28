-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataMetaTUnitTests.cs:342
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- Uses injected variable: o1
return o1 == o1
