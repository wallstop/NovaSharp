#! /usr/bin/lua
--
-- lua-TestMore : <http://fperrad.github.com/lua-TestMore/>
--
-- Copyright (C) 2009-2011, Perrad Francois
--
-- This code is licensed under the terms of the MIT/X11 license,
-- like Lua itself.
--

--[[

=head1 Lua Library

=head2 Synopsis

    % prove 320-stdin.t

=head2 Description

Tests Lua Basic & IO Libraries with stdin

=cut

--]]

require 'Test.More'

local lua = (platform and platform.lua) or arg[-1]
local stdin_helper = platform.stdin_helper

if not stdin_helper then
    if not pcall(io.popen, lua .. [[ -e "a=1"]]) then
        skip_all "io.popen not supported"
    end
end

plan(12)

local function run_with_stdin(chunk, input_path)
    if stdin_helper then
        return stdin_helper.run(chunk, input_path)
    end

    local cmd = lua .. [[ -e "]] .. chunk .. [["]] .. ' < ' .. input_path
    local pipe = io.popen(cmd)
    local lines = {}

    while true do
        local line = pipe:read'*l'
        if line == nil then
            break
        end

        lines[#lines + 1] = line
    end

    pipe:close()
    return lines
end

f = io.open('lib1.lua', 'w')
f:write[[
function norm (x, y)
    return (x^2 + y^2)^0.5
end

function twice (x)
    return 2*x
end
]]
f:close()

local chunk = [[
dofile();
n = norm(3.4, 1.0);
print(twice(n))
]]
local lines = run_with_stdin(chunk, 'lib1.lua')
like(lines[1], '^7%.088', "function dofile (stdin)")

os.remove('lib1.lua') -- clean up

f = io.open('foo.lua', 'w')
f:write[[
function foo (x)
    return x
end
]]
f:close()

chunk = [[
f = loadfile();
print(foo);
f();
print(foo('ok'))
]]
lines = run_with_stdin(chunk, 'foo.lua')
is(lines[1], 'nil', "function loadfile (stdin)")
is(lines[2], 'ok')

os.remove('foo.lua') -- clean up

f = io.open('file.txt', 'w')
f:write("file with text\n")
f:close()

chunk = [[
print(io.read'*l');
print(io.read'*l');
print(io.Type(io.stdin))
]]
lines = run_with_stdin(chunk, 'file.txt')
is(lines[1], 'file with text', "function io.read *l")
is(lines[2], 'nil')
is(lines[3], 'file')

f = io.open('number.txt', 'w')
f:write("6.0     -3.23   15e12\n")
f:write("4.3     234     1000001\n")
f:close()

chunk = [[
while true do
    local n1, n2, n3 = io.read('*number', '*number', '*number')
    if not n1 then break end
    print(math.max(n1, n2, n3))
end
]]
lines = run_with_stdin(chunk, 'number.txt')
is(lines[1], '15000000000000', "function io:read *number")
is(lines[2], '1000001')

os.remove('number.txt') -- clean up

chunk = [[
for line in io.lines() do
    print(line)
end
]]
lines = run_with_stdin(chunk, 'file.txt')
is(lines[1], 'file with text', "function io.lines")
is(lines[2], nil)

os.remove('file.txt') -- clean up

f = io.open('dbg.txt', 'w')
f:write("print 'ok'\n")
f:write("error 'dbg'\n")
f:write("cont\n")
f:close()

chunk = [[
debug.debug()
]]
lines = run_with_stdin(chunk, 'dbg.txt')
is(lines[1], 'ok', "function debug.debug")
is(lines[2], nil)

os.remove('dbg.txt') -- clean up


-- Local Variables:
--   mode: lua
--   lua-indent-level: 4
--   fill-column: 100
-- End:
-- vim: ft=lua expandtab shiftwidth=4:
