-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:428
-- @test: NumericForLoopTUnitTests.NonFiniteBoundsMatchReferenceIterations
-- Every case bounds its body with a break so effectively unbounded loops still terminate.
local function report(n)
    return n > 3 and "MANY" or tostring(n)
end

local n

n = 0
for i = 5, 0 / 0, -1 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))

n = 0
for i = 5.5, 0 / 0, -1 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))

n = 0
for i = 10, 1, 0 / 0 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))

n = 0
for i = 1, 0 / 0, 1.0 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))

n = 0
for i = 5, 0 / 0, 1 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))

n = 0
for i = 5, 2e400, -1 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))

n = 0
for i = 5.5, 2e400, -1 do
    n = n + 1
    if n > 3 then break end
end
print(report(n))
