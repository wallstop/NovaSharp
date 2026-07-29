-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Debugging/SourceCodeTUnitTests.cs:57
-- @test: SourceCodeTUnitTests.GetCodeSnippetAppendsIntermediateLines
local one = 1
local two = one + 1
local three = two + 1
return three
