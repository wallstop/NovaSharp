-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:250
-- @test: DynValueZStringTUnitTests.StringRepWithSeparatorUsesZString
-- @compat-notes: Test targets Lua 5.3+
return string.rep('ab', 3, '-')
