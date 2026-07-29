-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:44
-- @test: TableTUnitTests.TableMethod1
x = 0 a = { value = 1912, val = function(self, num) x = self.value + num end }
