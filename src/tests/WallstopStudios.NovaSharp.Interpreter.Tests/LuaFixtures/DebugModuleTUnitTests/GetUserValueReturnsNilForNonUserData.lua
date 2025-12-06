-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:280
-- @test: DebugModuleTUnitTests.GetUserValueReturnsNilForNonUserData
return debug.getuservalue('string')
