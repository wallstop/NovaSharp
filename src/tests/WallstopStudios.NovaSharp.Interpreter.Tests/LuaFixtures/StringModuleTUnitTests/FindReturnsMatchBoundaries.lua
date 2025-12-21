-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:757
-- @test: StringModuleTUnitTests.FindReturnsMatchBoundaries
-- @compat-notes: Test targets Lua 5.1
local startIndex, endIndex = string.find('NovaSharp', 'Sharp')
                return startIndex, endIndex
