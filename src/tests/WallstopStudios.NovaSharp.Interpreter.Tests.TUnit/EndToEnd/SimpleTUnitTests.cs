namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class SimpleTUnitTests
    {
        [global::TUnit.Core.Test]
        public void EmptyLongComment()
        {
            Script s = new(default(CoreModules));
            DynValue res = s.DoString("--[[]]");
        }

        [global::TUnit.Core.Test]
        public void EmptyChunk()
        {
            Script s = new(default(CoreModules));
            DynValue res = s.DoString("");
        }

        [global::TUnit.Core.Test]
        public async Task CSharpStaticFunctionCallStatement()
        {
            DynValue[] args = Array.Empty<DynValue>();

            string script = "print(\"hello\", \"world\");";

            Script s = new();

            s.Globals.Set(
                "print",
                DynValue.NewCallback(
                    new CallbackFunction(
                        (x, a) =>
                        {
                            args = a.GetArray();
                            return DynValue.NewNumber(1234.0);
                        }
                    )
                )
            );

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
            await Assert.That(args.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(args[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[0].String).IsEqualTo("hello").ConfigureAwait(false);
            await Assert.That(args[1].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[1].String).IsEqualTo("world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CSharpStaticFunctionCallRedef()
        {
            DynValue[] args = Array.Empty<DynValue>();

            string script = "local print = print; print(\"hello\", \"world\");";

            Script s = new();
            s.Globals.Set(
                "print",
                DynValue.NewCallback(
                    new CallbackFunction(
                        (x, a) =>
                        {
                            args = a.GetArray();
                            return DynValue.NewNumber(1234.0);
                        }
                    )
                )
            );

            DynValue res = s.DoString(script);

            await Assert.That(args.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(args[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[0].String).IsEqualTo("hello").ConfigureAwait(false);
            await Assert.That(args[1].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[1].String).IsEqualTo("world").ConfigureAwait(false);
            await Assert.That(res.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CSharpStaticFunctionCall4()
        {
            string script = "return callback()();";

            DynValue callback2 = DynValue.NewCallback(
                new CallbackFunction(
                    (x, a) =>
                    {
                        return DynValue.NewNumber(1234.0);
                    }
                )
            );
            DynValue callback = DynValue.NewCallback(
                new CallbackFunction(
                    (x, a) =>
                    {
                        return callback2;
                    }
                )
            );

            Script s = new();
            s.Globals.Set("callback", callback);

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1234.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CSharpStaticFunctionCall3()
        {
            string script = "return callback();";

            DynValue callback = DynValue.NewCallback(
                new CallbackFunction(
                    (x, a) =>
                    {
                        return DynValue.NewNumber(1234.0);
                    }
                )
            );

            Script s = new();
            s.Globals.Set("callback", callback);

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1234.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CSharpStaticFunctionCall2()
        {
            DynValue[] args = Array.Empty<DynValue>();

            string script = "return callback 'hello';";

            Script s = new();
            s.Globals.Set(
                "callback",
                DynValue.NewCallback(
                    new CallbackFunction(
                        (x, a) =>
                        {
                            args = a.GetArray();
                            return DynValue.NewNumber(1234.0);
                        }
                    )
                )
            );

            DynValue res = s.DoString(script);

            await Assert.That(args.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(args[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[0].String).IsEqualTo("hello").ConfigureAwait(false);
            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1234.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CSharpStaticFunctionCall()
        {
            DynValue[] args = Array.Empty<DynValue>();

            string script = "return print(\"hello\", \"world\");";

            Script s = new();
            s.Globals.Set(
                "print",
                DynValue.NewCallback(
                    new CallbackFunction(
                        (x, a) =>
                        {
                            args = a.GetArray();
                            return DynValue.NewNumber(1234.0);
                        }
                    )
                )
            );

            DynValue res = s.DoString(script);

            await Assert.That(args.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(args[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[0].String).IsEqualTo("hello").ConfigureAwait(false);
            await Assert.That(args[1].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(args[1].String).IsEqualTo("world").ConfigureAwait(false);
            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1234.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        //!!! DO NOT REFORMAT THIS METHOD !!!
        public async Task LongStrings()
        {
            string script =
                @"    
				x = [[
					ciao
				]];

				y = [=[ [[uh]] ]=];

				z = [===[[==[[=[[[eheh]]=]=]]===]

				return x,y,z";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert
                .That(res.Tuple[0].String)
                .IsEqualTo("\t\t\t\t\tciao\n\t\t\t\t")
                .ConfigureAwait(false);
            await Assert.That(res.Tuple[1].String).IsEqualTo(" [[uh]] ").ConfigureAwait(false);
            await Assert
                .That(res.Tuple[2].String)
                .IsEqualTo("[==[[=[[[eheh]]=]=]")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnicodeEscapeLua53Style()
        {
            string script =
                @"    
				x = 'ciao\u{41}';
				return x;";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.String).IsEqualTo("ciaoA").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InvalidEscape()
        {
            string script =
                @"    
				x = 'ciao\k{41}';
				return x;";

            await Assert
                .That(() => Script.RunString(script))
                .Throws<SyntaxErrorException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task KeywordsInStrings()
        {
            string keywrd =
                "and break do else elseif end false end for function end goto if ::in:: in local nil not [or][[][==][[]] repeat return { then 0 end return; }; then true (x != 5 or == * 3 - 5) x";

            string script =
                $@"    
				x = '{keywrd}';
				return x;";

            DynValue res = Script.RunString(script);
            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.String).IsEqualTo(keywrd).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParserErrorMessage()
        {
            bool caught = false;
            string script =
                @"    
				return 'It's a wet floor warning saying wheat flour instead. \
				Probably, the cook thought it was funny. \
				He was wrong.'";

            try
            {
                DynValue res = Script.RunString(script);
            }
            catch (SyntaxErrorException ex)
            {
                caught = true;
                await Assert.That(string.IsNullOrEmpty(ex.Message)).IsFalse().ConfigureAwait(false);
            }

            await Assert.That(caught).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StringsWithBackslashLineEndings2()
        {
            string script =
                @"    
				return 'a\
				b\
				c'";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StringsWithBackslashLineEndings()
        {
            string script =
                @"    
				return 'It is a wet floor warning saying wheat flour instead. \
				Probably, the cook thought it was funny. \
				He was wrong.'";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FunctionCallWrappers()
        {
            string script =
                @"    
				function boh(x) 
					return 1912 + x;
				end
			";

            Script s = new();
            s.DoString(script);

            DynValue res = s.Globals.Get("boh").Function.Call(82);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1994).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ReturnSimpleUnop()
        {
            string script = @"return -42";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(-42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ReturnSimple()
        {
            string script = @"return 42";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorSimple()
        {
            string script = @"return 6*7";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void SimpleBoolShortCircuit()
        {
            string script =
                @"    
				x = true or crash();
				y = false and crash();
			";

            Script s = new();
            s.Globals.Set(
                "crash",
                DynValue.NewCallback(
                    new CallbackFunction(
                        (x, a) =>
                        {
                            throw new InvalidOperationException("FAIL!");
                        }
                    )
                )
            );

            s.DoString(script);
        }

        [global::TUnit.Core.Test]
        public async Task FunctionOrOperator()
        {
            string script =
                @"    
				loadstring = loadstring or load;

				return loadstring;
			";

            Script s = new();
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SelectNegativeIndex()
        {
            string script =
                @"    
				return select(-1,'a','b','c');
			";

            Script s = new();
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.String).IsEqualTo("c").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BoolConversionAndShortCircuit()
        {
            string script =
                @"    
				i = 0;

				function f()
					i = i + 1;
					return '!';
				end					
				
				x = false;
				y = true;

				return false or f(), true or f(), false and f(), true and f(), i";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Tuple.Length).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(res.Tuple[3].Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.Tuple[4].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].String).IsEqualTo("!").ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Boolean).IsEqualTo(true).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Boolean).IsEqualTo(false).ConfigureAwait(false);
            await Assert.That(res.Tuple[3].String).IsEqualTo("!").ConfigureAwait(false);
            await Assert.That(res.Tuple[4].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void HanoiTowersDontCrash()
        {
            string script =
                @"
			function move(n, src, dst, via)
				if n > 0 then
					move(n - 1, src, via, dst)
					move(n - 1, via, dst, src)
				end
			end
 
			move(4, 1, 2, 3)
			";

            DynValue res = Script.RunString(script);
        }

        [global::TUnit.Core.Test]
        public async Task Factorial()
        {
            string script =
                @"    
				-- defines a factorial function
				function fact (n)
					if (n == 0) then
						return 1
					else
						return n*fact(n - 1)
					end
				end
    
				return fact(5)";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(120.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IfStatmWithScopeCheck()
        {
            string script =
                @"    
				x = 0

				if (x == 0) then
					local i = 3;
					x = i * 2;
				end
    
				return i, x";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ScopeBlockCheck()
        {
            string script =
                @"    
				local x = 6;
				
				do
					local i = 33;
				end
		
				return i, x";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForLoopWithBreak()
        {
            string script =
                @"    
				x = 0

				for i = 1, 10 do
					x = i
					break;
				end
    
				return x";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForEachLoopWithBreak()
        {
            string script =
                @"    
				x = 0
				y = 0

				t = { 2, 4, 6, 8, 10, 12 };

				function iter (a, ii)
				  ii = ii + 1
				  local v = a[ii]
				  if v then
					return ii, v
				  end
				end
    
				function ipairslua (a)
				  return iter, a, 0
				end

				for i,j in ipairslua(t) do
					x = x + i
					y = y + j

					if (i >= 3) then
						break
					end
				end
    
				return x, y";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(6).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForEachLoop()
        {
            string script =
                @"    
				x = 0
				y = 0

				t = { 2, 4, 6, 8, 10, 12 };

				function iter (a, ii)
				  ii = ii + 1
				  local v = a[ii]
				  if v then
					return ii, v
				  end
				end
    
				function ipairslua (a)
				  return iter, a, 0
				end

				for i,j in ipairslua(t) do
					x = x + i
					y = y + j
				end
    
				return x, y";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(21).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LengthOperator()
        {
            string script =
                @"    
				x = 'ciao'
				y = { 1, 2, 3 }
   
				return #x, #y";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForLoopWithBreakAndScopeCheck()
        {
            string script =
                @"    
				x = 0

				for i = 1, 10 do
					x = x + i

					if (i == 3) then
						break
					end
				end
    
				return i, x";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FactorialWithOneReturn()
        {
            string script =
                @"    
				-- defines a factorial function
				function fact (n)
					if (n == 0) then
						return 1
					end
					return n*fact(n - 1)
				end
    
				return fact(5)";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(120.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VeryBasic()
        {
            string script = @"return 7";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence1()
        {
            string script = @"return 1+2*3";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence2()
        {
            string script = @"return 2*3+1";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorAssociativity()
        {
            string script = @"return 2^3^2";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(512).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence3()
        {
            string script = @"return 5-3-2";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence4()
        {
            string script = @"return 3 + -1";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence5()
        {
            string script = @"return 3 * -1 + 5 * 3";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence6()
        {
            string script = @"return -2^2";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(-4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedence7()
        {
            string script = @"return -7 / 0.5";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(-14).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorPrecedenceAndAssociativity()
        {
            string script = @"return 5+3*7-2*5+2^3^2";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(528).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OperatorParenthesis()
        {
            string script = @"return (5+3)*7-2*5+(2^3)^2";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(110).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GlobalVarAssignment()
        {
            string script = @"x = 1; return x;";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TupleAssignment1()
        {
            string script =
                @"    
				function y()
					return 2, 3
				end

				function x()
					return 1, y()
				end

				w, x, y, z = 0, x()
    
				return w+x+y+z";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IterativeFactorialWithWhile()
        {
            string script =
                @"    
				function fact (n)
					local result = 1;
					while(n > 0) do
						result = result * n;
						n = n - 1;
					end
					return result;
				end
    
				return fact(5)";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(120.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IterativeFactorialWithRepeatUntilAndScopeCheck()
        {
            string script =
                @"    
				function fact (n)
					local result = 1;
					repeat
						local checkscope = 1;
						result = result * n;
						n = n - 1;
					until (n == 0 and checkscope == 1)
					return result;
				end
    
				return fact(5)";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(120.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SimpleForLoop()
        {
            string script =
                @"    
					x = 0
					for i = 1, 3 do
						x = x + i;
					end

					return x;
			";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(6.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SimpleFunc()
        {
            string script =
                @"    
				function fact (n)
					return 3;
				end
    
				return fact(3)";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IterativeFactorialWithFor()
        {
            string script =
                @"    
				-- defines a factorial function
				function fact (n)
					x = 1
					for i = n, 1, -1 do
						x = x * i;
					end

					return x;
				end
    
				return fact(5)";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(120.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LocalFunctionsObscureScopeRule()
        {
            string script =
                @"    
				local function fact()
					return fact;
				end

				return fact();
				";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Function).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FunctionWithStringArg2()
        {
            string script =
                @"    
				x = 0;

				fact = function(y)
					x = y
				end

				fact 'ciao';

				return x;
				";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.String).IsEqualTo("ciao").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FunctionWithStringArg()
        {
            string script =
                @"    
				x = 0;

				function fact(y)
					x = y
				end

				fact 'ciao';

				return x;
				";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.String).IsEqualTo("ciao").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FunctionWithTableArg()
        {
            string script =
                @"    
				x = 0;

				function fact(y)
					x = y
				end

				fact { 1,2,3 };

				return x;
				";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TupleAssignment2()
        {
            string script =
                @"    
				function boh()
					return 1, 2;
				end

				x,y,z = boh(), boh()

				return x,y,z;
				";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void LoopWithReturn()
        {
            string script =
                @"function Allowed( )
									for i = 1, 20 do
  										return false 
									end
									return true
								end
						Allowed();
								";

            DynValue res = Script.RunString(script);
        }

        [global::TUnit.Core.Test]
        public void IfWithLongExpr()
        {
            string script =
                @"function Allowed( )
									for i = 1, 20 do
									if ( false ) or ( true and true ) or ( 7+i <= 9 and false ) then 
  										return false 
									end
									end		
									return true
								end
						Allowed();
								";

            DynValue res = Script.RunString(script);
        }

        [global::TUnit.Core.Test]
        public void IfWithLongExprTbl()
        {
            string script =
                @"
						t = { {}, {} }
						
						function Allowed( )
									for i = 1, 20 do
									if ( t[1][3] ) or ( i <= 17 and t[1][1] ) or ( 7+i <= 9 and t[1][1] ) then 
  										return false 
									end
									end		
									return true
								end
						Allowed();
								";

            DynValue res = Script.RunString(script);
        }

        [global::TUnit.Core.Test]
        public async Task ExpressionReducesTuples()
        {
            string script =
                @"
					function x()
						return 1,2
					end

					do return (x()); end
					do return x(); end
								";

            DynValue res = (new Script(CoreModulePresets.Default)).DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpressionReducesTuples2()
        {
            string script =
                @"
					function x()
						return 3,4
					end

					return 1,x(),x()
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ArgsDoNotChange()
        {
            string script =
                @"
					local a = 1;
					local b = 2;

					function x(c, d)
						c = c + 3;
						d = d + 4;
						return c + d;
					end

					return x(a, b+1), a, b;
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(11).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsNoError()
        {
            string script =
                @"
					function x(...)

					end

					function y(a, ...)

					end

					return 1;
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsSum()
        {
            string script =
                @"
					function x(...)
						local t = table.pack(...);
						local sum = 0;

						for i = 1, #t do
							sum = sum + t[i];
						end
	
						return sum;
					end

					return x(1,2,3,4);
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsSum2()
        {
            string script =
                @"
					function x(m, ...)
						local t = table.pack(...);
						local sum = 0;

						for i = 1, #t do
							sum = sum + t[i];
						end
	
						return sum * m;
					end

					return x(5,1,2,3,4);
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(50).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsSumTb()
        {
            string script =
                @"
					function x(...)
						local t = {...};
						local sum = 0;

						for i = 1, #t do
							sum = sum + t[i];
						end
	
						return sum;
					end

					return x(1,2,3,4);
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SwapPattern()
        {
            string script =
                @"
					local n1 = 1
					local n2 = 2
					local n3 = 3
					local n4 = 4
					n1,n2,n3,n4 = n4,n3,n2,n1

					return n1,n2,n3,n4;
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[3].Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SwapPatternGlobal()
        {
            string script =
                @"
					n1 = 1
					n2 = 2
					n3 = 3
					n4 = 4
					n1,n2,n3,n4 = n4,n3,n2,n1

					return n1,n2,n3,n4;
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(res.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(res.Tuple[0].Number).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(res.Tuple[1].Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(res.Tuple[2].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(res.Tuple[3].Number).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EnvTestSuite()
        {
            string script =
                @"
				local RES = { }

				RES.T1 = (_ENV == _G) 

				a = 1

				local function f(t)
				  local _ENV = t 

				  RES.T2 = (getmetatable == nil) 
  
				  a = 2 -- create a new entry in t, doesn't touch the original 'a' global
				  b = 3 -- create a new entry in t
				end

				local t = {}
				f(t)

				RES.T3 = a;
				RES.T4 = b;
				RES.T5 = t.a;
				RES.T6 = t.b;

				return RES;
								";

            DynValue res = Script.RunString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);

            Table t = res.Table;

            await Assert.That(t.Get("T1").Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(t.Get("T1").Boolean).IsEqualTo(true).ConfigureAwait(false);

            await Assert.That(t.Get("T2").Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(t.Get("T2").Boolean).IsEqualTo(true).ConfigureAwait(false);

            await Assert.That(t.Get("T3").Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(t.Get("T3").Number).IsEqualTo(1).ConfigureAwait(false);

            await Assert.That(t.Get("T4").Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);

            await Assert.That(t.Get("T5").Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(t.Get("T5").Number).IsEqualTo(2).ConfigureAwait(false);

            await Assert.That(t.Get("T6").Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(t.Get("T6").Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TupleToOperator()
        {
            string script =
                @"    
				function x()
					return 3, 'xx';
				end

				return x() == 3;	
			";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(res.Boolean).IsEqualTo(true).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LiteralExpands()
        {
            string script =
                @"    
				x = 'a\65\66\67z';
				return x;	
			";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(res.String).IsEqualTo("aABCz").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HomonymArguments()
        {
            string script =
                @"    
				function test(_,value,_) return _; end

				return test(1, 2, 3);	
			";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsSumMainChunk()
        {
            string script =
                @"
					local t = table.pack(...);
					local sum = 0;

					for i = 1, #t do
						sum = sum + t[i];
					end
	
					return sum;
								";

            DynValue fn = new Script().LoadString(script);

            DynValue res = fn.Function.Call(1, 2, 3, 4);

            await Assert.That(res.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(res.Number).IsEqualTo(10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsInNoVarArgsReturnsError()
        {
            string script =
                @"
					function x()
						local t = {...};
						local sum = 0;

						for i = 1, #t do
							sum = sum + t[i];
						end
	
						return sum;
					end

					return x(1,2,3,4);
								";

            await Assert
                .That(() => Script.RunString(script))
                .Throws<SyntaxErrorException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HexFloats1()
        {
            string script = "return 0x0.1E";
            DynValue result = Script.RunString(script);
            await Assert
                .That(result.Number)
                .IsEqualTo((double)0x1E / (double)0x100)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HexFloats2()
        {
            string script = "return 0xA23p-4";
            DynValue result = Script.RunString(script);
            await Assert.That(result.Number).IsEqualTo((double)0xA23 / 16.0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HexFloats3()
        {
            string script = "return 0X1.921FB54442D18P+1";
            DynValue result = Script.RunString(script);
            await Assert
                .That(result.Number)
                .IsEqualTo((1 + (double)0x921FB54442D18 / (double)0x10000000000000) * 2)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SimpleDelegateInterop1()
        {
            int a = 3;
            Script script = new() { Globals = { ["action"] = new Action(() => a = 5) } };
            script.DoString("action()");
            await Assert.That(a).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SimpleDelegateInterop2()
        {
            using UserDataRegistrationPolicyScope policyScope =
                UserDataRegistrationPolicyScope.Override(InteropRegistrationPolicy.Automatic);

            int a = 3;
            Script script = new() { Globals = { ["action"] = new Action(() => a = 5) } };
            script.DoString("action()");
            await Assert.That(a).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void MissingArgsDefaultToNil()
        {
            Script s = new(default(CoreModules));
            DynValue res = s.DoString(
                @"
				function test(a)
					return a;
				end

				test();
				"
            );
        }

        [global::TUnit.Core.Test]
        public void ParsingTest()
        {
            Script s = new(default(CoreModules));
            DynValue res = s.LoadString(
                @"
				t = {'a', 'b', 'c', ['d'] = 'f', ['e'] = 5, [65] = true, [true] = false}
				function myFunc()
				  return 'one', 'two'
				end

				print('Table Test 1:')
				for k,v in pairs(t) do
				  print(tostring(k) .. ' / ' .. tostring(v))
				end
				print('Table Test 2:')
				for X,X in pairs(t) do
				  print(tostring(X) .. ' / ' .. tostring(X))
				end
				print('Function Test 1:')
				v1,v2 = myFunc()
				print(v1)
				print(v2)
				print('Function Test 2:')
				v,v = myFunc()
				print(v)
				print(v)
				"
            );
        }

        //		[global::TUnit.Core.Test]
        //		public void TestModulesLoadingWithoutCrash()
        //		{
        //#if !PCL
        //			var basePath = AppDomain.CurrentDomain.BaseDirectory;
        //			var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts\\test");
        //			Script script = new Script();

        //			((ScriptLoaderBase)script.Options.ScriptLoader).ModulePaths = new[]
        //			{
        //				System.IO.Path.Combine(basePath, "scripts\\test\\test.lua"),
        //			};
        //			var obj = script.LoadFile(System.IO.Path.Combine(scriptPath, "test.lua"));
        //			obj.Function.Call();
        //#endif
        //		}

        [global::TUnit.Core.Test]
        public async Task NumericConversionFailsIfOutOfBounds()
        {
            Script s = new()
            {
                Globals = { ["my_function_takes_byte"] = (Action<byte>)(p => { }) },
            };

            await Assert
                .That(() =>
                    s.DoString(
                        "my_function_takes_byte(2010191) -- a huge number that is definitely not a byte"
                    )
                )
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }
    }
}
