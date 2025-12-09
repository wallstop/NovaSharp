-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:522
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithOutParamsInterleavedWithDifferentMethods
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: bitwise OR; Uses injected variable: obj
local results = {}
                
                -- First: void with out
                local nil1, a, b = obj.VoidWithOut(1, 2)
                table.insert(results, tostring(nil1) .. '|' .. a .. '|' .. b)
                
                -- Second: regular method
                local sum = obj.AddNumbers(3, 4)
                table.insert(results, tostring(sum))
                
                -- Third: void with out again
                local nil2, c, d = obj.VoidWithOut(5, 6)
                table.insert(results, tostring(nil2) .. '|' .. c .. '|' .. d)
                
                -- Fourth: ref/out combo
                local upper, concat, lower = obj.ManipulateString('X', 'Y')
                table.insert(results, upper .. '|' .. concat .. '|' .. lower)
                
                -- Fifth: void with out again
                local nil3, e, f = obj.VoidWithOut(7, 8)
                table.insert(results, tostring(nil3) .. '|' .. e .. '|' .. f)
                
                return table.concat(results, ';')
