-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:277
-- @test: LoadModuleTUnitTests.LoadReturnsTupleWithSyntaxErrorWhenStringIsInvalidLua52Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: load with string arg (5.2+)
return load('function(')
