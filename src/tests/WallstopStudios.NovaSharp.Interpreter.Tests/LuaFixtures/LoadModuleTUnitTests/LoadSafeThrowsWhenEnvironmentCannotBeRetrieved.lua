-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleTUnitTests.cs:348
-- @test: LoadModuleTUnitTests.LoadSafeThrowsWhenEnvironmentCannotBeRetrieved
-- @compat-notes: NovaSharp-only: loadsafe is a NovaSharp extension; Lua 5.1 only: _ENV is always captured
-- This test is for Lua 5.1 only, where _ENV is always captured for setfenv/getfenv compatibility.
-- In Lua 5.2+, if the calling function doesn't reference globals, it won't have _ENV as an upvalue,
-- and loadsafe will successfully find the global environment from the script's globals.
local original_env = _ENV
local ls = loadsafe
local pc = pcall
_ENV = nil
-- In Lua 5.1, even though we only use locals, _ENV is still captured as an upvalue.
-- So when loadsafe looks for _ENV, it finds nil and fails.
local ok, err = pc(function() return ls('return 1') end)
_ENV = original_env
return ok, err
