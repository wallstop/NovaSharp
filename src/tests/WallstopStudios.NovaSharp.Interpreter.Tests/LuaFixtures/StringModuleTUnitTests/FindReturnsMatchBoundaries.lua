-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:491
-- @test: StringModuleTUnitTests.FindReturnsMatchBoundaries
-- @compat-notes: Lua 5.3+: bitwise operators
local startIndex, endIndex = string.find('NovaSharp', 'Sharp')
                return startIndex, endIndex
