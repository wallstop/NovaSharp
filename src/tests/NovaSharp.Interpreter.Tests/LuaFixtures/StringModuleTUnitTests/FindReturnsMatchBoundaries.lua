-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:232
-- @test: StringModuleTUnitTests.FindReturnsMatchBoundaries
-- @compat-notes: Lua 5.3+: bitwise operators
local startIndex, endIndex = string.find('NovaSharp', 'Sharp')
                return startIndex, endIndex
