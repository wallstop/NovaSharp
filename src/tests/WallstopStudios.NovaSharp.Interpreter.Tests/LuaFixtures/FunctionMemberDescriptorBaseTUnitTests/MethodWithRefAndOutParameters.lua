-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\FunctionMemberDescriptorBaseTUnitTests.cs:120
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithRefAndOutParameters
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: bitwise OR; Uses injected variable: obj
local upper, concat, lower = obj.ManipulateString('Hello', 'World')
                return upper .. '|' .. concat .. '|' .. lower
