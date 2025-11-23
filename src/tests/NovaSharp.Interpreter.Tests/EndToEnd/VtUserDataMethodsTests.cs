namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Compatibility;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NUnit.Framework;

    [TestFixture]
    public class VtUserDataMethodsTests
    {
        public struct SomeClassNoRegister : IComparable
        {
            public string ManipulateString(
                string input,
                ref string tobeconcat,
                out string lowercase
            )
            {
                tobeconcat = input + tobeconcat;
                lowercase = input.ToLower();
                return input.ToUpper();
            }

            public string ConcatNums(int p1, int p2)
            {
                return $"{p1}%{p2}";
            }

            public int SomeMethodWithLongName(int i)
            {
                return i * 2;
            }

            public static StringBuilder SetComplexRecursive(List<int[]> intList)
            {
                StringBuilder sb = new();

                foreach (int[] arr in intList)
                {
                    sb.Append(string.Join(",", arr.Select(s => s.ToString()).ToArray()));
                    sb.Append("|");
                }

                return sb;
            }

            public static StringBuilder SetComplexTypes(
                List<string> strlist,
                IList<int> intlist,
                Dictionary<string, int> map,
                string[] strarray,
                int[] intarray
            )
            {
                StringBuilder sb = new();

                sb.Append(string.Join(",", strlist.ToArray()));

                sb.Append("|");

                sb.Append(string.Join(",", intlist.Select(i => i.ToString()).ToArray()));

                sb.Append("|");

                sb.Append(string.Join(",", map.Keys.OrderBy(x => x.ToUpperInvariant()).ToArray()));

                sb.Append("|");

                sb.Append(
                    string.Join(",", map.Values.OrderBy(x => x).Select(i => i.ToString()).ToArray())
                );

                sb.Append("|");

                sb.Append(string.Join(",", strarray));

                sb.Append("|");

                sb.Append(string.Join(",", intarray.Select(i => i.ToString()).ToArray()));

                return sb;
            }

            public static StringBuilder ConcatS(
                int p1,
                string p2,
                IComparable p3,
                bool p4,
                List<object> p5,
                IEnumerable<object> p6,
                StringBuilder p7,
                Dictionary<object, object> p8,
                SomeClassNoRegister p9,
                int p10 = 1994
            )
            {
                p7.Append(p1);
                p7.Append(p2);
                p7.Append(p3);
                p7.Append(p4);

                p7.Append("|");
                foreach (object o in p5)
                {
                    p7.Append(o);
                }

                p7.Append("|");
                foreach (object o in p6)
                {
                    p7.Append(o);
                }

                p7.Append("|");
                foreach (object o in p8.Keys.OrderBy(x => x.ToString().ToUpperInvariant()))
                {
                    p7.Append(o);
                }

                p7.Append("|");
                foreach (object o in p8.Values.OrderBy(x => x.ToString().ToUpperInvariant()))
                {
                    p7.Append(o);
                }

                p7.Append("|");

                p7.Append(p9);
                p7.Append(p10);

                return p7;
            }

            public string Format(string s, params object[] args)
            {
                return FormatUnchecked(s, args);
            }

            public StringBuilder ConcatI(
                Script s,
                int p1,
                string p2,
                IComparable p3,
                bool p4,
                List<object> p5,
                IEnumerable<object> p6,
                StringBuilder p7,
                Dictionary<object, object> p8,
                SomeClassNoRegister p9,
                int p10 = 1912
            )
            {
                Assert.That(s, Is.Not.Null);
                return ConcatS(p1, p2, p3, p4, p5, p6, p7, p8, this, p10);
            }

            public override string ToString()
            {
                return "!SOMECLASS!";
            }

            public List<int> MkList(int from, int to)
            {
                List<int> l = new();
                for (int i = from; i <= to; i++)
                {
                    l.Add(i);
                }

                return l;
            }

            public int CompareTo(object obj)
            {
                throw new NotImplementedException();
            }
        }

        public struct SomeClass : IComparable
        {
            public string ManipulateString(
                string input,
                ref string tobeconcat,
                out string lowercase
            )
            {
                tobeconcat = input + tobeconcat;
                lowercase = input.ToLower();
                return input.ToUpper();
            }

            public string ConcatNums(int p1, int p2)
            {
                return $"{p1}%{p2}";
            }

            public int SomeMethodWithLongName(int i)
            {
                return i * 2;
            }

            public static StringBuilder SetComplexRecursive(List<int[]> intList)
            {
                StringBuilder sb = new();

                foreach (int[] arr in intList)
                {
                    sb.Append(string.Join(",", arr.Select(s => s.ToString()).ToArray()));
                    sb.Append("|");
                }

                return sb;
            }

            public static StringBuilder SetComplexTypes(
                List<string> strlist,
                IList<int> intlist,
                Dictionary<string, int> map,
                string[] strarray,
                int[] intarray
            )
            {
                StringBuilder sb = new();

                sb.Append(string.Join(",", strlist.ToArray()));

                sb.Append("|");

                sb.Append(string.Join(",", intlist.Select(i => i.ToString()).ToArray()));

                sb.Append("|");

                sb.Append(string.Join(",", map.Keys.OrderBy(x => x.ToUpperInvariant()).ToArray()));

                sb.Append("|");

                sb.Append(
                    string.Join(",", map.Values.OrderBy(x => x).Select(i => i.ToString()).ToArray())
                );

                sb.Append("|");

                sb.Append(string.Join(",", strarray));

                sb.Append("|");

                sb.Append(string.Join(",", intarray.Select(i => i.ToString()).ToArray()));

                return sb;
            }

            public static StringBuilder ConcatS(
                int p1,
                string p2,
                IComparable p3,
                bool p4,
                List<object> p5,
                IEnumerable<object> p6,
                StringBuilder p7,
                Dictionary<object, object> p8,
                SomeClass p9,
                int p10 = 1994
            )
            {
                p7.Append(p1);
                p7.Append(p2);
                p7.Append(p3);
                p7.Append(p4);

                p7.Append("|");
                foreach (object o in p5)
                {
                    p7.Append(o);
                }

                p7.Append("|");
                foreach (object o in p6)
                {
                    p7.Append(o);
                }

                p7.Append("|");
                foreach (object o in p8.Keys.OrderBy(x => x.ToString().ToUpperInvariant()))
                {
                    p7.Append(o);
                }

                p7.Append("|");
                foreach (object o in p8.Values.OrderBy(x => x.ToString().ToUpperInvariant()))
                {
                    p7.Append(o);
                }

                p7.Append("|");

                p7.Append(p9);
                p7.Append(p10);

                return p7;
            }

            public string Format(string s, params object[] args)
            {
                return FormatUnchecked(s, args);
            }

            public StringBuilder ConcatI(
                Script s,
                int p1,
                string p2,
                IComparable p3,
                bool p4,
                List<object> p5,
                IEnumerable<object> p6,
                StringBuilder p7,
                Dictionary<object, object> p8,
                SomeClass p9,
                int p10 = 1912
            )
            {
                Assert.That(s, Is.Not.Null);
                return ConcatS(p1, p2, p3, p4, p5, p6, p7, p8, this, p10);
            }

            public override string ToString()
            {
                return "!SOMECLASS!";
            }

            public List<int> MkList(int from, int to)
            {
                List<int> l = new();
                for (int i = from; i <= to; i++)
                {
                    l.Add(i);
                }

                return l;
            }

            public int CompareTo(object obj)
            {
                throw new NotImplementedException();
            }
        }

        public interface INterface1
        {
            public string Test1();
        }

        public interface INterface2
        {
            public string Test2();
        }

        public class SomeOtherClass
        {
            public string Test1()
            {
                return "Test1";
            }

            public string Test2()
            {
                return "Test2";
            }
        }

        public struct SomeOtherClassCustomDescriptor { }

        public struct CustomDescriptor : IUserDataDescriptor
        {
            public string Name
            {
                get { return "ciao"; }
            }

            public Type Type
            {
                get { return typeof(SomeOtherClassCustomDescriptor); }
            }

            public DynValue Index(Script script, object obj, DynValue index, bool dummy)
            {
                return DynValue.NewNumber(index.Number * 4);
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool dummy
            )
            {
                throw new NotImplementedException();
            }

            public string AsString(object obj)
            {
                return null;
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return null;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                return Framework.Do.IsInstanceOfType(type, obj);
            }
        }

        public struct SelfDescribingClass : IUserDataType
        {
            public DynValue Index(Script script, DynValue index, bool isNameIndex)
            {
                return DynValue.NewNumber(index.Number * 3);
            }

            public bool SetIndex(Script script, DynValue index, DynValue value, bool isNameIndex)
            {
                throw new NotImplementedException();
            }

            public DynValue MetaIndex(Script script, string metaname)
            {
                throw new NotImplementedException();
            }
        }

        public struct SomeOtherClassWithDualInterfaces : INterface1, INterface2
        {
            public string Test1()
            {
                return "Test1";
            }

            public string Test2()
            {
                return "Test2";
            }
        }

        public void TestVarArgs(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
			return myobj.format('{0}.{1}@{2}:{3}', 1, 2, 'ciao', true);";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("1.2@ciao:True"));
        }

        public void TestConcatMethodStaticComplexCustomConv(InteropAccessMode opt)
        {
            try
            {
                UserData.UnregisterType<SomeClass>();

                string script =
                    @"    
				strlist = { 'ciao', 'hello', 'aloha' };
				intlist = {  };
				dictry = { ciao = 39, hello = 78, aloha = 128 };
				
				x = static.SetComplexTypes(strlist, intlist, dictry, strlist, intlist);

				return x;";

                Script s = new();

                SomeClass obj = new();

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                Script.GlobalOptions.CustomConverters.Clear();

                Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
                    DataType.Table,
                    typeof(List<string>),
                    v => null
                );

                Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
                    DataType.Table,
                    typeof(IList<int>),
                    v => new List<int>() { 42, 77, 125, 13 }
                );

                Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
                    DataType.Table,
                    typeof(int[]),
                    v => new int[] { 43, 78, 126, 14 }
                );

                Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<StringBuilder>(
                    (s, v) => DynValue.NewString(v.ToString().ToUpper())
                );

                s.Globals.Set("static", UserData.CreateStatic<SomeClass>());
                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                Assert.That(res.Type, Is.EqualTo(DataType.String));
                Assert.That(
                    res.String,
                    Is.EqualTo(
                        "CIAO,HELLO,ALOHA|42,77,125,13|ALOHA,CIAO,HELLO|39,78,128|CIAO,HELLO,ALOHA|43,78,126,14"
                    )
                );
            }
            finally
            {
                Script.GlobalOptions.CustomConverters.Clear();
            }
        }

        public void TestConcatMethodStaticComplex(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				strlist = { 'ciao', 'hello', 'aloha' };
				intlist = { 42, 77, 125, 13 };
				dictry = { ciao = 39, hello = 78, aloha = 128 };
				
				x = static.SetComplexTypes(strlist, intlist, dictry, strlist, intlist);

				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("static", UserData.CreateStatic<SomeClass>());
            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(
                res.String,
                Is.EqualTo(
                    "ciao,hello,aloha|42,77,125,13|aloha,ciao,hello|39,78,128|ciao,hello,aloha|42,77,125,13"
                )
            );
        }

        public void TestConcatMethodStaticComplexRec(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				array = { { 1, 2, 3 }, { 11, 35, 77 }, { 16, 42, 64 }, {99, 76, 17 } };				
			
				x = static.SetComplexRecursive(array);

				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("static", UserData.CreateStatic<SomeClass>());
            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("1,2,3|11,35,77|16,42,64|99,76,17|"));
        }

        public void TestRefOutParams(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				x, y, z = myobj:manipulateString('CiAo', 'hello');
				return x, y, z;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("static", UserData.CreateStatic<SomeClass>());
            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple.Length, Is.EqualTo(3));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[2].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[0].String, Is.EqualTo("CIAO"));
            Assert.That(res.Tuple[1].String, Is.EqualTo("CiAohello"));
            Assert.That(res.Tuple[2].String, Is.EqualTo("ciao"));
        }

        public void TestConcatMethodStatic(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = static.ConcatS(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("static", UserData.CreateStatic<SomeClass>());
            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(
                res.String,
                Is.EqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1994"
                )
            );
        }

        public void TestConcatMethod(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = myobj.ConcatI(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(
                res.String,
                Is.EqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1912"
                )
            );
        }

        public void TestConcatMethodSemicolon(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = myobj:ConcatI(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(
                res.String,
                Is.EqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1912"
                )
            );
        }

        public void TestConstructorAndConcatMethodSemicolon(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				myobj = mytype.__new();
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = myobj:ConcatI(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

            Script s = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals["mytype"] = typeof(SomeClass);

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(
                res.String,
                Is.EqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1912"
                )
            );
        }

        public void TestConcatMethodStaticSimplifiedSyntax(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = static.ConcatS(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals["static"] = typeof(SomeClass);
            s.Globals["myobj"] = obj;

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(
                res.String,
                Is.EqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1994"
                )
            );
        }

        public void TestDelegateMethod(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				x = concat(1, 2);
				return x;";

            Script s = new();

            SomeClass obj = new();

            s.Globals["concat"] = CallbackFunction.FromDelegate(
                s,
                (Func<int, int, string>)obj.ConcatNums,
                opt
            );

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("1%2"));
        }

        public void TestListMethod(InteropAccessMode opt)
        {
            string script =
                @"    
				x = mklist(1, 4);
				sum = 0;				

				for _, v in ipairs(x) do
					sum = sum + v;
				end

				return sum;";

            Script s = new();

            SomeClassNoRegister obj = new();

            s.Globals["mklist"] = CallbackFunction.FromDelegate(
                s,
                (Func<int, int, List<int>>)obj.MkList,
                opt
            );

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(10));
        }

        [Test]
        public void VInteropConcatMethodNone()
        {
            TestConcatMethod(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodLazy()
        {
            TestConcatMethod(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodPrecomputed()
        {
            TestConcatMethod(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConcatMethodSemicolonNone()
        {
            TestConcatMethodSemicolon(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodSemicolonLazy()
        {
            TestConcatMethodSemicolon(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodSemicolonPrecomputed()
        {
            TestConcatMethodSemicolon(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConstructorAndConcatMethodSemicolonNone()
        {
            TestConstructorAndConcatMethodSemicolon(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConstructorAndConcatMethodSemicolonLazy()
        {
            TestConstructorAndConcatMethodSemicolon(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConstructorAndConcatMethodSemicolonPrecomputed()
        {
            TestConstructorAndConcatMethodSemicolon(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxCustomConvNone()
        {
            TestConcatMethodStaticComplexCustomConv(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxCustomConvLazy()
        {
            TestConcatMethodStaticComplexCustomConv(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxCustomConvPrecomputed()
        {
            TestConcatMethodStaticComplexCustomConv(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxNone()
        {
            TestConcatMethodStaticComplex(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxLazy()
        {
            TestConcatMethodStaticComplex(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxPrecomputed()
        {
            TestConcatMethodStaticComplex(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxRecNone()
        {
            TestConcatMethodStaticComplexRec(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxRecLazy()
        {
            TestConcatMethodStaticComplexRec(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticCplxRecPrecomputed()
        {
            TestConcatMethodStaticComplexRec(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticNone()
        {
            TestConcatMethodStatic(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodStaticLazy()
        {
            TestConcatMethodStatic(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticPrecomputed()
        {
            TestConcatMethodStatic(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticSimplifiedSyntaxNone()
        {
            TestConcatMethodStaticSimplifiedSyntax(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConcatMethodStaticSimplifiedSyntaxLazy()
        {
            TestConcatMethodStaticSimplifiedSyntax(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConcatMethodStaticSimplifiedSyntaxPrecomputed()
        {
            TestConcatMethodStaticSimplifiedSyntax(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropVarArgsNone()
        {
            TestVarArgs(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropVarArgsLazy()
        {
            TestVarArgs(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropVarArgsPrecomputed()
        {
            TestVarArgs(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropDelegateMethodNone()
        {
            TestDelegateMethod(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropDelegateMethodLazy()
        {
            TestDelegateMethod(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropDelegateMethodPrecomputed()
        {
            TestDelegateMethod(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropOutRefParamsNone()
        {
            TestRefOutParams(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropOutRefParamsLazy()
        {
            TestRefOutParams(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropOutRefParamsPrecomputed()
        {
            TestRefOutParams(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropListMethodNone()
        {
            TestListMethod(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropListMethodLazy()
        {
            TestListMethod(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropListMethodPrecomputed()
        {
            TestListMethod(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropTestAutoregisterPolicy()
        {
            IRegistrationPolicy oldPolicy = UserData.RegistrationPolicy;

            try
            {
                string script = @"return myobj:Test1()";

                UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;

                Script s = new();

                SomeOtherClass obj = new();

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                Assert.That(res.Type, Is.EqualTo(DataType.String));
                Assert.That(res.String, Is.EqualTo("Test1"));
            }
            finally
            {
                UserData.RegistrationPolicy = oldPolicy;
            }
        }

        [Test]
        public void VInteropDualInterfaces()
        {
            string script = @"return myobj:Test1() .. myobj:Test2()";

            Script s = new();

            UserData.UnregisterType<INterface1>();
            UserData.UnregisterType<INterface2>();
            UserData.RegisterType<INterface1>();
            UserData.RegisterType<INterface2>();

            SomeOtherClassWithDualInterfaces obj = new();

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Test1Test2"));
        }

        [Test]
        public void VInteropTestNamesCamelized()
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				a = myobj:SomeMethodWithLongName(1);
				b = myobj:someMethodWithLongName(2);
				c = myobj:some_method_with_long_name(3);
				d = myobj:Some_method_withLong_name(4);
				
				return a + b + c + d;
			";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>();

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(20));
        }

        [Test]
        public void VInteropTestSelfDescribingType()
        {
            UserData.UnregisterType<SelfDescribingClass>();

            string script =
                @"    
				a = myobj[1];
				b = myobj[2];
				c = myobj[3];
				
				return a + b + c;
			";

            Script s = new();

            SelfDescribingClass obj = new();

            UserData.UnregisterType<SelfDescribingClass>();
            UserData.RegisterType<SelfDescribingClass>();

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(18));
        }

        [Test]
        public void VInteropTestCustomDescribedType()
        {
            UserData.UnregisterType<SomeOtherClassCustomDescriptor>();

            string script =
                @"    
				a = myobj[1];
				b = myobj[2];
				c = myobj[3];
				
				return a + b + c;
			";

            Script s = new();

            SomeOtherClassCustomDescriptor obj = new();

            UserData.RegisterType<SomeOtherClassCustomDescriptor>(new CustomDescriptor());

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(24));
        }

        private static string FormatUnchecked(string format, object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(format, args);
        }
    }
}
