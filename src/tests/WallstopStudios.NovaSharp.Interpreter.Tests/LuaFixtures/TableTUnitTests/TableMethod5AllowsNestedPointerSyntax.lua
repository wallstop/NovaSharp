-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:92
-- @test: TableTUnitTests.TableMethod5AllowsNestedPointerSyntax
x = 0 a = { value = 1912 } b = { tb = a } c = { tb = b }
