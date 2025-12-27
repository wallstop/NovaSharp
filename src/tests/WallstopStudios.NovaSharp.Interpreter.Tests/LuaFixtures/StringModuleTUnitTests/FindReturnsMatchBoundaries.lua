-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:521
-- @test: StringModuleTUnitTests.FindReturnsMatchBoundaries
local startIndex, endIndex = string.find('NovaSharp', 'Sharp')
                return startIndex, endIndex
