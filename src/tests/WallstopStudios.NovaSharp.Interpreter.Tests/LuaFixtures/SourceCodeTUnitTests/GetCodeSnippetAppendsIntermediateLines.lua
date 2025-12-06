-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/SourceCodeTUnitTests.cs:50
-- @test: SourceCodeTUnitTests.GetCodeSnippetAppendsIntermediateLines
-- @compat-notes: Lua 5.3+: bitwise operators
local one = 1
local two = one + 1
local three = two + 1
return three
