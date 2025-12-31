-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleTUnitTests.cs:381
-- @test: LoadModuleTUnitTests.LoadSafeUsesGlobalEnvWhenCallerHasNoEnvUpvalue
-- @compat-notes: NovaSharp-only: loadsafe is a NovaSharp extension; Lua 5.2+ only
-- In Lua 5.2+, if the calling function doesn't reference any globals, it won't have _ENV
-- as an upvalue. In this case, loadsafe should successfully use the script's global environment.
local ls = loadsafe
-- This function only uses the local 'ls', so it has no _ENV upvalue in Lua 5.2+.
-- loadsafe should fall back to using the script's global environment.
local fn = ls('return 42')
return fn()
