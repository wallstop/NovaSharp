-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:358
-- @test: StringModuleTUnitTests.GMatchIteratesOverMatches
-- @compat-notes: Lua 5.3+: bitwise operators
local iter = string.gmatch('one two', '%w+')
                return iter(), iter()
