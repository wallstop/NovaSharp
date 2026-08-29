-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:98
-- @test: StringModuleTUnitTests.RegisteredStringCallbacksUseArgumentViews
local iterator = string.gmatch('alpha beta', '%a+')
local matches = {}
for value in iterator do
    matches[#matches + 1] = value
end
local upper = string.upper(matches[1])
local substring = string.sub(matches[2], 2)
local formatted = string.format('%s:%d', 'n', 2)
assert(upper == 'ALPHA')
assert(substring == 'eta')
assert(formatted == 'n:2')
return iterator, upper, substring, formatted
