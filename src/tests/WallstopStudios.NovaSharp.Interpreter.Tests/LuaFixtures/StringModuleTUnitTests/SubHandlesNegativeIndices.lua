-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:832
-- @test: StringModuleTUnitTests.SubHandlesNegativeIndices
-- @compat-notes: Test targets Lua 5.1
return string.sub('NovaSharp', -5, -2)
