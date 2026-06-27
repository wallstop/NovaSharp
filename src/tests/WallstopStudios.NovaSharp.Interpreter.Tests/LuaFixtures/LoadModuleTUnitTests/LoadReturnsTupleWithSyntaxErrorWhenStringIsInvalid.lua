-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleTUnitTests.cs:165
-- @test: LoadModuleTUnitTests.LoadReturnsTupleWithSyntaxErrorWhenStringIsInvalid
-- Lua 5.2+: load with string arg (5.2+)
return load('function(')
