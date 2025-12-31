local function make()
    local captured = 7
    local function inner()
        return captured
    end
    return inner
end
local fn = make()
print("Index 1:", debug.getupvalue(fn, 1))
print("Index 2:", debug.getupvalue(fn, 2))
print("Index 3:", debug.getupvalue(fn, 3))
