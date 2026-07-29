-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1338
-- @test: SimpleTUnitTests.FunctionCallExpressionPositionsAdjustReturnArity
local statementCount = 0

local function values()
    return 'a', nil, 'c'
end

local function observe(...)
    statementCount = statementCount + select('#', ...)
    return 'ignored', nil, 'ignored-again'
end

observe(values())
observe(values(), 'tail')

local prefixOnly, tail, missing1, missing2 = values(), 'tail'
local head, expandedFirst, expandedSecond, expandedThird = 'head', values()
local grouped = (values())

assert(prefixOnly == 'a', 'non-final function call scalarized')
assert(tail == 'tail', 'literal after scalarized call')
assert(missing1 == nil, 'missing assignment target one')
assert(missing2 == nil, 'missing assignment target two')
assert(head == 'head', 'literal before expanded call')
assert(expandedFirst == 'a', 'expanded first')
assert(expandedSecond == nil, 'expanded nil')
assert(expandedThird == 'c', 'expanded third')
assert(grouped == 'a', 'parenthesized call scalarized')
assert(statementCount == 5, 'statement calls adjusted argument arity')

return prefixOnly,
    tail,
    missing1,
    missing2,
    head,
    expandedFirst,
    expandedSecond,
    expandedThird,
    grouped,
    statementCount
