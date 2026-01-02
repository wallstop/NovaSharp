-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\SimpleTUnitTests.cs:1380
-- @test: SimpleTUnitTests.VarArgsSum
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: table.pack (5.2+)
function x(...)
						local t = table.pack(...);
						local sum = 0;

						for i = 1, #t do
							sum = sum + t[i];
						end
	
						return sum;
					end

					return x(1,2,3,4);
