-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\UserDataMetaTUnitTests.cs:409
-- @test: ArithmOperatorsTestClass.InteropMetaComparisons
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: o2
return o2 >= o2
