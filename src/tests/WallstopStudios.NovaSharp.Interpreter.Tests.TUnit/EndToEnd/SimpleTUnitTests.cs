namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
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
        public void CSharpStaticFunctionCallStatement()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Void));
            Assert.That(args.Length, Is.EqualTo(2));
            Assert.That(args[0].Type, Is.EqualTo(DataType.String));
            Assert.That(args[0].String, Is.EqualTo("hello"));
            Assert.That(args[1].Type, Is.EqualTo(DataType.String));
            Assert.That(args[1].String, Is.EqualTo("world"));
        }

        [global::TUnit.Core.Test]
        public void CSharpStaticFunctionCallRedef()
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

            Assert.That(args.Length, Is.EqualTo(2));
            Assert.That(args[0].Type, Is.EqualTo(DataType.String));
            Assert.That(args[0].String, Is.EqualTo("hello"));
            Assert.That(args[1].Type, Is.EqualTo(DataType.String));
            Assert.That(args[1].String, Is.EqualTo("world"));
            Assert.That(res.Type, Is.EqualTo(DataType.Void));
        }

        [global::TUnit.Core.Test]
        public void CSharpStaticFunctionCall4()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1234.0));
        }

        [global::TUnit.Core.Test]
        public void CSharpStaticFunctionCall3()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1234.0));
        }

        [global::TUnit.Core.Test]
        public void CSharpStaticFunctionCall2()
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

            Assert.That(args.Length, Is.EqualTo(1));
            Assert.That(args[0].Type, Is.EqualTo(DataType.String));
            Assert.That(args[0].String, Is.EqualTo("hello"));
            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1234.0));
        }

        [global::TUnit.Core.Test]
        public void CSharpStaticFunctionCall()
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

            Assert.That(args.Length, Is.EqualTo(2));
            Assert.That(args[0].Type, Is.EqualTo(DataType.String));
            Assert.That(args[0].String, Is.EqualTo("hello"));
            Assert.That(args[1].Type, Is.EqualTo(DataType.String));
            Assert.That(args[1].String, Is.EqualTo("world"));
            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1234.0));
        }

        [global::TUnit.Core.Test]
        //!!! DO NOT REFORMAT THIS METHOD !!!
        public void LongStrings()
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

            Assert.That(res.Tuple.Length, Is.EqualTo(3));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[2].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[0].String, Is.EqualTo("\t\t\t\t\tciao\n\t\t\t\t"));
            Assert.That(res.Tuple[1].String, Is.EqualTo(" [[uh]] "));
            Assert.That(res.Tuple[2].String, Is.EqualTo("[==[[=[[[eheh]]=]=]"));
        }

        [global::TUnit.Core.Test]
        public void UnicodeEscapeLua53Style()
        {
            string script =
                @"    
				x = 'ciao\u{41}';
				return x;";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("ciaoA"));
        }

        [global::TUnit.Core.Test]
        public void InvalidEscape()
        {
            string script =
                @"    
				x = 'ciao\k{41}';
				return x;";

            Assert.Throws<SyntaxErrorException>(() => Script.RunString(script));
        }

        [global::TUnit.Core.Test]
        public void KeywordsInStrings()
        {
            string keywrd =
                "and break do else elseif end false end for function end goto if ::in:: in local nil not [or][[][==][[]] repeat return { then 0 end return; }; then true (x != 5 or == * 3 - 5) x";

            string script =
                $@"    
				x = '{keywrd}';
				return x;";

            DynValue res = Script.RunString(script);
            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo(keywrd));
        }

        [global::TUnit.Core.Test]
        public void ParserErrorMessage()
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
                Assert.That(string.IsNullOrEmpty(ex.Message), Is.False, ex.Message);
            }

            Assert.That(caught, Is.True);
        }

        [global::TUnit.Core.Test]
        public void StringsWithBackslashLineEndings2()
        {
            string script =
                @"    
				return 'a\
				b\
				c'";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
        }

        [global::TUnit.Core.Test]
        public void StringsWithBackslashLineEndings()
        {
            string script =
                @"    
				return 'It is a wet floor warning saying wheat flour instead. \
				Probably, the cook thought it was funny. \
				He was wrong.'";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
        }

        [global::TUnit.Core.Test]
        public void FunctionCallWrappers()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1994));
        }

        [global::TUnit.Core.Test]
        public void ReturnSimpleUnop()
        {
            string script = @"return -42";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(-42));
        }

        [global::TUnit.Core.Test]
        public void ReturnSimple()
        {
            string script = @"return 42";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(42));
        }

        [global::TUnit.Core.Test]
        public void OperatorSimple()
        {
            string script = @"return 6*7";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(42));
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
        public void FunctionOrOperator()
        {
            string script =
                @"    
				loadstring = loadstring or load;

				return loadstring;
			";

            Script s = new();
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.ClrFunction));
        }

        [global::TUnit.Core.Test]
        public void SelectNegativeIndex()
        {
            string script =
                @"    
				return select(-1,'a','b','c');
			";

            Script s = new();
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("c"));
        }

        [global::TUnit.Core.Test]
        public void BoolConversionAndShortCircuit()
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

            Assert.That(res.Tuple.Length, Is.EqualTo(5));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Boolean));
            Assert.That(res.Tuple[2].Type, Is.EqualTo(DataType.Boolean));
            Assert.That(res.Tuple[3].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[4].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[0].String, Is.EqualTo("!"));
            Assert.That(res.Tuple[1].Boolean, Is.EqualTo(true));
            Assert.That(res.Tuple[2].Boolean, Is.EqualTo(false));
            Assert.That(res.Tuple[3].String, Is.EqualTo("!"));
            Assert.That(res.Tuple[4].Number, Is.EqualTo(2));
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
        public void Factorial()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(120.0));
        }

        [global::TUnit.Core.Test]
        public void IfStatmWithScopeCheck()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Nil));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(6));
        }

        [global::TUnit.Core.Test]
        public void ScopeBlockCheck()
        {
            string script =
                @"    
				local x = 6;
				
				do
					local i = 33;
				end
		
				return i, x";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Nil));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(6));
        }

        [global::TUnit.Core.Test]
        public void ForLoopWithBreak()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1));
        }

        [global::TUnit.Core.Test]
        public void ForEachLoopWithBreak()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(6));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(12));
        }

        [global::TUnit.Core.Test]
        public void ForEachLoop()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(21));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(42));
        }

        [global::TUnit.Core.Test]
        public void LengthOperator()
        {
            string script =
                @"    
				x = 'ciao'
				y = { 1, 2, 3 }
   
				return #x, #y";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(4));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(3));
        }

        [global::TUnit.Core.Test]
        public void ForLoopWithBreakAndScopeCheck()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(2));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Nil));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(6));
        }

        [global::TUnit.Core.Test]
        public void FactorialWithOneReturn()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(120.0));
        }

        [global::TUnit.Core.Test]
        public void VeryBasic()
        {
            string script = @"return 7";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(7));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence1()
        {
            string script = @"return 1+2*3";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(7));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence2()
        {
            string script = @"return 2*3+1";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(7));
        }

        [global::TUnit.Core.Test]
        public void OperatorAssociativity()
        {
            string script = @"return 2^3^2";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(512));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence3()
        {
            string script = @"return 5-3-2";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(0));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence4()
        {
            string script = @"return 3 + -1";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(2));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence5()
        {
            string script = @"return 3 * -1 + 5 * 3";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(12));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence6()
        {
            string script = @"return -2^2";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(-4));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedence7()
        {
            string script = @"return -7 / 0.5";
            Script s = new(default(CoreModules));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(-14));
        }

        [global::TUnit.Core.Test]
        public void OperatorPrecedenceAndAssociativity()
        {
            string script = @"return 5+3*7-2*5+2^3^2";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(528));
        }

        [global::TUnit.Core.Test]
        public void OperatorParenthesis()
        {
            string script = @"return (5+3)*7-2*5+(2^3)^2";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(110));
        }

        [global::TUnit.Core.Test]
        public void GlobalVarAssignment()
        {
            string script = @"x = 1; return x;";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1));
        }

        [global::TUnit.Core.Test]
        public void TupleAssignment1()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(6));
        }

        [global::TUnit.Core.Test]
        public void IterativeFactorialWithWhile()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(120.0));
        }

        [global::TUnit.Core.Test]
        public void IterativeFactorialWithRepeatUntilAndScopeCheck()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(120.0));
        }

        [global::TUnit.Core.Test]
        public void SimpleForLoop()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(6.0));
        }

        [global::TUnit.Core.Test]
        public void SimpleFunc()
        {
            string script =
                @"    
				function fact (n)
					return 3;
				end
    
				return fact(3)";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(3));
        }

        [global::TUnit.Core.Test]
        public void IterativeFactorialWithFor()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(120.0));
        }

        [global::TUnit.Core.Test]
        public void LocalFunctionsObscureScopeRule()
        {
            string script =
                @"    
				local function fact()
					return fact;
				end

				return fact();
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Function));
        }

        [global::TUnit.Core.Test]
        public void FunctionWithStringArg2()
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

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("ciao"));
        }

        [global::TUnit.Core.Test]
        public void FunctionWithStringArg()
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

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("ciao"));
        }

        [global::TUnit.Core.Test]
        public void FunctionWithTableArg()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Table));
        }

        [global::TUnit.Core.Test]
        public void TupleAssignment2()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[2].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(1));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(1));
            Assert.That(res.Tuple[2].Number, Is.EqualTo(2));
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
        public void ExpressionReducesTuples()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1));
        }

        [global::TUnit.Core.Test]
        public void ExpressionReducesTuples2()
        {
            string script =
                @"
					function x()
						return 3,4
					end

					return 1,x(),x()
								";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(4));
        }

        [global::TUnit.Core.Test]
        public void ArgsDoNotChange()
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

            Assert.That(res.Tuple.Length, Is.EqualTo(3));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[2].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(11));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(1));
            Assert.That(res.Tuple[2].Number, Is.EqualTo(2));
        }

        [global::TUnit.Core.Test]
        public void VarArgsNoError()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1));
        }

        [global::TUnit.Core.Test]
        public void VarArgsSum()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(10));
        }

        [global::TUnit.Core.Test]
        public void VarArgsSum2()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(50));
        }

        [global::TUnit.Core.Test]
        public void VarArgsSumTb()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(10));
        }

        [global::TUnit.Core.Test]
        public void SwapPattern()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(4));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(4));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(3));
            Assert.That(res.Tuple[2].Number, Is.EqualTo(2));
            Assert.That(res.Tuple[3].Number, Is.EqualTo(1));
        }

        [global::TUnit.Core.Test]
        public void SwapPatternGlobal()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(4));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(4));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(3));
            Assert.That(res.Tuple[2].Number, Is.EqualTo(2));
            Assert.That(res.Tuple[3].Number, Is.EqualTo(1));
        }

        [global::TUnit.Core.Test]
        public void EnvTestSuite()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Table));

            Table t = res.Table;

            Assert.That(t.Get("T1").Type, Is.EqualTo(DataType.Boolean), "T1-Type");
            Assert.That(t.Get("T1").Boolean, Is.EqualTo(true), "T1-Val");

            Assert.That(t.Get("T2").Type, Is.EqualTo(DataType.Boolean), "T2-Type");
            Assert.That(t.Get("T2").Boolean, Is.EqualTo(true), "T2-Val");

            Assert.That(t.Get("T3").Type, Is.EqualTo(DataType.Number), "T3-Type");
            Assert.That(t.Get("T3").Number, Is.EqualTo(1), "T3-Val");

            Assert.That(t.Get("T4").Type, Is.EqualTo(DataType.Nil), "T4");

            Assert.That(t.Get("T5").Type, Is.EqualTo(DataType.Number), "T5-Type");
            Assert.That(t.Get("T5").Number, Is.EqualTo(2), "T5-Val");

            Assert.That(t.Get("T6").Type, Is.EqualTo(DataType.Number), "T6-Type");
            Assert.That(t.Get("T6").Number, Is.EqualTo(3), "T6-Val");
        }

        [global::TUnit.Core.Test]
        public void TupleToOperator()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(res.Boolean, Is.EqualTo(true));
        }

        [global::TUnit.Core.Test]
        public void LiteralExpands()
        {
            string script =
                @"    
				x = 'a\65\66\67z';
				return x;	
			";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("aABCz"));
        }

        [global::TUnit.Core.Test]
        public void HomonymArguments()
        {
            string script =
                @"    
				function test(_,value,_) return _; end

				return test(1, 2, 3);	
			";

            Script s = new(default(CoreModules));
            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(3));
        }

        [global::TUnit.Core.Test]
        public void VarArgsSumMainChunk()
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

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(10));
        }

        [global::TUnit.Core.Test]
        public void VarArgsInNoVarArgsReturnsError()
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

            Assert.Throws<SyntaxErrorException>(() => Script.RunString(script));
        }

        [global::TUnit.Core.Test]
        public void HexFloats1()
        {
            string script = "return 0x0.1E";
            DynValue result = Script.RunString(script);
            Assert.That(result.Number, Is.EqualTo((double)0x1E / (double)0x100));
        }

        [global::TUnit.Core.Test]
        public void HexFloats2()
        {
            string script = "return 0xA23p-4";
            DynValue result = Script.RunString(script);
            Assert.That(result.Number, Is.EqualTo((double)0xA23 / 16.0));
        }

        [global::TUnit.Core.Test]
        public void HexFloats3()
        {
            string script = "return 0X1.921FB54442D18P+1";
            DynValue result = Script.RunString(script);
            Assert.That(
                result.Number,
                Is.EqualTo((1 + (double)0x921FB54442D18 / (double)0x10000000000000) * 2)
            );
        }

        [global::TUnit.Core.Test]
        public void SimpleDelegateInterop1()
        {
            int a = 3;
            Script script = new() { Globals = { ["action"] = new Action(() => a = 5) } };
            script.DoString("action()");
            Assert.That(a, Is.EqualTo(5));
        }

        [global::TUnit.Core.Test]
        public void SimpleDelegateInterop2()
        {
            using UserDataRegistrationPolicyScope policyScope =
                UserDataRegistrationPolicyScope.Override(InteropRegistrationPolicy.Automatic);

            int a = 3;
            Script script = new() { Globals = { ["action"] = new Action(() => a = 5) } };
            script.DoString("action()");
            Assert.That(a, Is.EqualTo(5));
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
        public void NumericConversionFailsIfOutOfBounds()
        {
            Script s = new()
            {
                Globals = { ["my_function_takes_byte"] = (Action<byte>)(p => { }) },
            };

            try
            {
                s.DoString(
                    "my_function_takes_byte(2010191) -- a huge number that is definitely not a byte"
                );

                Assert.Fail(); // ScriptRuntimeException should have been thrown, if it doesn't Assert.Fail should execute
            }
            catch (ScriptRuntimeException)
            {
                //Assert.Pass(e.DecoratedMessage);
            }
        }
    }
}
