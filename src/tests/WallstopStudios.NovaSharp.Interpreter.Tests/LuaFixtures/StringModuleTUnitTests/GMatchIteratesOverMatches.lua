-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:695
-- @test: StringModuleTUnitTests.GMatchIteratesOverMatches
-- @compat-notes: Lua 5.3+: bitwise operators
local iter = string.gmatch('one two', '%w+')
                return iter(), iter()
