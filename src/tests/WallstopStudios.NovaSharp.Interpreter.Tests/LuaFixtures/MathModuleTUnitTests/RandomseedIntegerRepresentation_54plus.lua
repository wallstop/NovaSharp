-- @lua-versions: 5.4
-- @expects-error: true
-- Tests that math.randomseed(x) requires integer representation in Lua 5.4+
-- Per Lua 5.4 manual §6.7: math.randomseed argument must have integer representation
-- NOTE: Lua 5.3 does NOT require integer representation for randomseed

math.randomseed(1.5)
print("ERROR: Should have thrown")
