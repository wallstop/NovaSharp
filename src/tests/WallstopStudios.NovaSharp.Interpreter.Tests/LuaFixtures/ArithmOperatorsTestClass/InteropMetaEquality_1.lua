-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataMetaTUnitTests.cs:339
-- @test: ArithmOperatorsTestClass.InteropMetaEquality
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: o1
return o1 != o2
