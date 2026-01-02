-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\DataTypes\DynValueZStringTUnitTests.cs:239
-- @test: DynValueZStringTUnitTests.StringRepUsesZStringInternally
-- @compat-notes: Test targets Lua 5.3+
return string.rep('ab', 5)
