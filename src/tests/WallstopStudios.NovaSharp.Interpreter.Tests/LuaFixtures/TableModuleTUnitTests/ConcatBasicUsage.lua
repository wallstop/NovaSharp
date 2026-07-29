-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:595
-- @test: TableModuleTUnitTests.ConcatBasicUsage
local t = {'a', 'b', 'c', 'd'}
                return table.concat(t)
