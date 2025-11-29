#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Compatibility;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    [ScriptGlobalOptionsIsolation]
    public sealed class UserDataMethodsTUnitTests
    {
        private static readonly int[] ScriptToClrIntArray = { 43, 78, 126, 14 };

        internal sealed class SomeClassNoRegister : IComparable
        {
            private int _instanceTouchCounter;
            private string _lastConcatResult = string.Empty;
            private string _lastFormattedString = string.Empty;

            public string ManipulateString(
                string input,
                ref string tobeconcat,
                out string lowercase
            )
            {
                int length = 0;
                if (input != null)
                {
                    length = input.Length;
                }
                _instanceTouchCounter += length;
                tobeconcat = input + tobeconcat;
                lowercase = InvariantString.ToLowerInvariantIfNeeded(input);
                return input.ToUpperInvariant();
            }

            public string ConcatNums(int p1, int p2)
            {
                string result = $"{p1}%{p2}";
                _lastConcatResult = result;
                return result;
            }

            public int SomeMethodWithLongName(int i)
            {
                _instanceTouchCounter ^= i;
                return i * 2;
            }

            public static StringBuilder SetComplexRecursive(List<int[]> intList)
            {
                StringBuilder sb = new();

                foreach (int[] arr in intList)
                {
                    sb.Append(JoinInts(arr));
                    sb.Append('|');
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

                sb.Append('|');

                sb.Append(JoinInts(intlist));

                sb.Append('|');

                sb.Append(string.Join(",", map.Keys.OrderBy(x => x, StringComparer.Ordinal)));

                sb.Append('|');

                sb.Append(JoinInts(map.Values.OrderBy(x => x)));

                sb.Append('|');

                sb.Append(string.Join(",", strarray));

                sb.Append('|');

                sb.Append(JoinInts(intarray));

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

                p7.Append('|');
                foreach (object o in p5)
                {
                    p7.Append(o);
                }

                p7.Append('|');
                foreach (object o in p6)
                {
                    p7.Append(o);
                }

                p7.Append('|');
                foreach (object o in p8.Keys.OrderBy(x => x.ToString()))
                {
                    p7.Append(o);
                }

                p7.Append('|');
                foreach (object o in p8.Values.OrderBy(x => x.ToString()))
                {
                    p7.Append(o);
                }

                p7.Append('|');

                p7.Append(p9);
                p7.Append(p10);

                return p7;
            }

            public string Format(string s, params object[] args)
            {
                string formatted = FormatUnchecked(s, args);
                _lastFormattedString = formatted;
                return formatted;
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
                if (s == null)
                {
                    throw new InvalidOperationException("Script instance cannot be null.");
                }
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

                _instanceTouchCounter += l.Count;
                return l;
            }

            public int CompareTo(object obj)
            {
                throw new NotImplementedException();
            }

            private static string JoinInts(IEnumerable<int> values)
            {
                return string.Join(
                    ",",
                    values.Select(i => i.ToString(CultureInfo.InvariantCulture))
                );
            }
        }

        internal sealed class SomeClass : IComparable
        {
            private int _instanceTouchCounter;
            private string _lastConcatResult = string.Empty;
            private string _lastFormattedString = string.Empty;

            public string ManipulateString(
                string input,
                ref string tobeconcat,
                out string lowercase
            )
            {
                int length = 0;
                if (input != null)
                {
                    length = input.Length;
                }
                _instanceTouchCounter += length;
                tobeconcat = input + tobeconcat;
                lowercase = InvariantString.ToLowerInvariantIfNeeded(input);
                return input.ToUpperInvariant();
            }

            public string ConcatNums(int p1, int p2)
            {
                string result = $"{p1}%{p2}";
                _lastConcatResult = result;
                return result;
            }

            public int SomeMethodWithLongName(int i)
            {
                _instanceTouchCounter ^= i;
                return i * 2;
            }

            public static StringBuilder SetComplexRecursive(List<int[]> intList)
            {
                StringBuilder sb = new();

                foreach (int[] arr in intList)
                {
                    sb.Append(JoinInts(arr));
                    sb.Append('|');
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

                sb.Append('|');

                sb.Append(JoinInts(intlist));

                sb.Append('|');

                sb.Append(string.Join(",", map.Keys.OrderBy(x => x, StringComparer.Ordinal)));

                sb.Append('|');

                sb.Append(JoinInts(map.Values.OrderBy(x => x)));

                sb.Append('|');

                sb.Append(string.Join(",", strarray));

                sb.Append('|');

                sb.Append(JoinInts(intarray));

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

                p7.Append('|');
                foreach (object o in p5)
                {
                    p7.Append(o);
                }

                p7.Append('|');
                foreach (object o in p6)
                {
                    p7.Append(o);
                }

                p7.Append('|');
                foreach (object o in p8.Keys.OrderBy(x => x.ToString().ToUpperInvariant()))
                {
                    p7.Append(o);
                }

                p7.Append('|');
                foreach (object o in p8.Values.OrderBy(x => x.ToString().ToUpperInvariant()))
                {
                    p7.Append(o);
                }

                p7.Append('|');

                p7.Append(p9);
                p7.Append(p10);

                return p7;
            }

            public string Format(string s, params object[] args)
            {
                string formatted = FormatUnchecked(s, args);
                _lastFormattedString = formatted;
                return formatted;
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
                if (s == null)
                {
                    throw new InvalidOperationException("Script instance cannot be null.");
                }
                return ConcatS(p1, p2, p3, p4, p5, p6, p7, p8, this, p10);
            }

            public override string ToString()
            {
                return "!SOMECLASS!";
            }

            private static string JoinInts(IEnumerable<int> values)
            {
                return string.Join(
                    ",",
                    values.Select(i => i.ToString(CultureInfo.InvariantCulture))
                );
            }

            public List<int> MkList(int from, int to)
            {
                List<int> l = new();
                for (int i = from; i <= to; i++)
                {
                    l.Add(i);
                }

                _instanceTouchCounter += l.Count;
                return l;
            }

            public int CompareTo(object obj)
            {
                throw new NotImplementedException();
            }
        }

        internal interface INterface1
        {
            public string Test1();
        }

        internal interface INterface2
        {
            public string Test2();
        }

        internal sealed class SomeOtherClass
        {
            private readonly string _test1Response = "Test1";
            private readonly string _test2Response = "Test2";

            public string Test1()
            {
                return _test1Response;
            }

            public string Test2()
            {
                return _test2Response;
            }
        }

        internal sealed class SomeOtherClassCustomDescriptor { }

        internal sealed class CustomDescriptor : IUserDataDescriptor
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

        internal sealed class SelfDescribingClass : IUserDataType
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

        internal sealed class SomeOtherClassWithDualInterfaces : INterface1, INterface2
        {
            private readonly string _test1Response = "Test1";
            private readonly string _test2Response = "Test2";

            public string Test1()
            {
                return _test1Response;
            }

            public string Test2()
            {
                return _test2Response;
            }
        }

        private static async Task TestVarArgsAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert.That(res.String).IsEqualTo("1.2@ciao:True");
        }

        private static async Task TestConcatMethodStaticComplexCustomConvAsync(
            InteropAccessMode opt
        )
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
                    v => (int[])ScriptToClrIntArray.Clone()
                );

                Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<StringBuilder>(
                    (s, v) => DynValue.NewString(v.ToString().ToUpperInvariant())
                );

                s.Globals.Set("static", UserData.CreateStatic<SomeClass>());
                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                await Assert.That(res.Type).IsEqualTo(DataType.String);
                await Assert
                    .That(res.String)
                    .IsEqualTo(
                        "CIAO,HELLO,ALOHA|42,77,125,13|ALOHA,CIAO,HELLO|39,78,128|CIAO,HELLO,ALOHA|43,78,126,14"
                    );
            }
            finally
            {
                Script.GlobalOptions.CustomConverters.Clear();
            }
        }

        private static async Task TestConcatMethodStaticComplexAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert
                .That(res.String)
                .IsEqualTo(
                    "ciao,hello,aloha|42,77,125,13|aloha,ciao,hello|39,78,128|ciao,hello,aloha|42,77,125,13"
                );
        }

        private static async Task TestConcatMethodStaticComplexRecAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert.That(res.String).IsEqualTo("1,2,3|11,35,77|16,42,64|99,76,17|");
        }

        private static async Task TestRefOutParamsAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(res.Tuple.Length).IsEqualTo(3);
            await Assert.That(res.Tuple[0].Type).IsEqualTo(DataType.String);
            await Assert.That(res.Tuple[1].Type).IsEqualTo(DataType.String);
            await Assert.That(res.Tuple[2].Type).IsEqualTo(DataType.String);
            await Assert.That(res.Tuple[0].String).IsEqualTo("CIAO");
            await Assert.That(res.Tuple[1].String).IsEqualTo("CiAohello");
            await Assert.That(res.Tuple[2].String).IsEqualTo("ciao");
        }

        private static async Task TestConcatMethodStaticAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert
                .That(res.String)
                .IsEqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1994"
                );
        }

        private static async Task TestConcatMethodAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert
                .That(res.String)
                .IsEqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1912"
                );
        }

        private static async Task TestConcatMethodSemicolonAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert
                .That(res.String)
                .IsEqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1912"
                );
        }

        private static async Task TestConstructorAndConcatMethodSemicolonAsync(
            InteropAccessMode opt
        )
        {
            UserData.UnregisterType<SomeClass>();

            string script =
                @"    
				myobj = mytype.__new();
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = myobj:ConcatI(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals["mytype"] = typeof(SomeClass);

            DynValue res = s.DoString(script);

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert
                .That(res.String)
                .IsEqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1912"
                );
        }

        private static async Task TestConcatMethodStaticSimplifiedSyntaxAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert
                .That(res.String)
                .IsEqualTo(
                    "eheh1ciao!SOMECLASS!True|asdqwezxc|asdqwezxc|123xy|asdqweXYzxc|!SOMECLASS!1994"
                );
        }

        private static async Task TestDelegateMethodAsync(InteropAccessMode opt)
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert.That(res.String).IsEqualTo("1%2");
        }

        private static async Task TestListMethodAsync(InteropAccessMode opt)
        {
            UserData.UnregisterType<SomeClass>();

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

            await Assert.That(res.Type).IsEqualTo(DataType.Number);
            await Assert.That(res.Number).IsEqualTo(10);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodNone()
        {
            return TestConcatMethodAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodLazy()
        {
            return TestConcatMethodAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodPrecomputed()
        {
            return TestConcatMethodAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodSemicolonNone()
        {
            return TestConcatMethodSemicolonAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodSemicolonLazy()
        {
            return TestConcatMethodSemicolonAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodSemicolonPrecomputed()
        {
            return TestConcatMethodSemicolonAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstructorAndConcatMethodSemicolonNone()
        {
            return TestConstructorAndConcatMethodSemicolonAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstructorAndConcatMethodSemicolonLazy()
        {
            return TestConstructorAndConcatMethodSemicolonAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstructorAndConcatMethodSemicolonPrecomputed()
        {
            return TestConstructorAndConcatMethodSemicolonAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxCustomConvNone()
        {
            return TestConcatMethodStaticComplexCustomConvAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxCustomConvLazy()
        {
            return TestConcatMethodStaticComplexCustomConvAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxCustomConvPrecomputed()
        {
            return TestConcatMethodStaticComplexCustomConvAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxNone()
        {
            return TestConcatMethodStaticComplexAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxLazy()
        {
            return TestConcatMethodStaticComplexAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxPrecomputed()
        {
            return TestConcatMethodStaticComplexAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxRecNone()
        {
            return TestConcatMethodStaticComplexRecAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxRecLazy()
        {
            return TestConcatMethodStaticComplexRecAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticCplxRecPrecomputed()
        {
            return TestConcatMethodStaticComplexRecAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticNone()
        {
            return TestConcatMethodStaticAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticLazy()
        {
            return TestConcatMethodStaticAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticPrecomputed()
        {
            return TestConcatMethodStaticAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticSimplifiedSyntaxNone()
        {
            return TestConcatMethodStaticSimplifiedSyntaxAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticSimplifiedSyntaxLazy()
        {
            return TestConcatMethodStaticSimplifiedSyntaxAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConcatMethodStaticSimplifiedSyntaxPrecomputed()
        {
            return TestConcatMethodStaticSimplifiedSyntaxAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropVarArgsNone()
        {
            return TestVarArgsAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropVarArgsLazy()
        {
            return TestVarArgsAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropVarArgsPrecomputed()
        {
            return TestVarArgsAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropDelegateMethodNone()
        {
            return TestDelegateMethodAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropDelegateMethodLazy()
        {
            return TestDelegateMethodAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropDelegateMethodPrecomputed()
        {
            return TestDelegateMethodAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropOutRefParamsNone()
        {
            return TestRefOutParamsAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropOutRefParamsLazy()
        {
            return TestRefOutParamsAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropOutRefParamsPrecomputed()
        {
            return TestRefOutParamsAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropListMethodNone()
        {
            return TestListMethodAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropListMethodLazy()
        {
            return TestListMethodAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropListMethodPrecomputed()
        {
            return TestListMethodAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public async Task InteropTestAutoregisterPolicy()
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

                await Assert.That(res.Type).IsEqualTo(DataType.String);
                await Assert.That(res.String).IsEqualTo("Test1");
            }
            finally
            {
                UserData.RegistrationPolicy = oldPolicy;
            }
        }

        [global::TUnit.Core.Test]
        public async Task InteropDualInterfaces()
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

            await Assert.That(res.Type).IsEqualTo(DataType.String);
            await Assert.That(res.String).IsEqualTo("Test1Test2");
        }

        [global::TUnit.Core.Test]
        public async Task InteropTestNamesCamelized()
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

            await Assert.That(res.Type).IsEqualTo(DataType.Number);
            await Assert.That(res.Number).IsEqualTo(20);
        }

        [global::TUnit.Core.Test]
        public async Task InteropTestSelfDescribingType()
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

            await Assert.That(res.Type).IsEqualTo(DataType.Number);
            await Assert.That(res.Number).IsEqualTo(18);
        }

        [global::TUnit.Core.Test]
        public async Task InteropTestCustomDescribedType()
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

            await Assert.That(res.Type).IsEqualTo(DataType.Number);
            await Assert.That(res.Number).IsEqualTo(24);
        }

        [global::TUnit.Core.Test]
        public async Task InteropStaticInstanceAccessRaisesError()
        {
            try
            {
                UserData.UnregisterType<SomeClass>();

                string script =
                    @"    
				t = { 'asd', 'qwe', 'zxc', ['x'] = 'X', ['y'] = 'Y' };
				x = mystatic.ConcatI(1, 'ciao', myobj, true, t, t, 'eheh', t, myobj);
				return x;";

                Script s = new();

                SomeClass obj = new();

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>();

                s.Globals.Set("mystatic", UserData.CreateStatic<SomeClass>());
                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                throw new InvalidOperationException(
                    "Expected ScriptRuntimeException when accessing instance members through a static descriptor."
                );
            }
            catch (ScriptRuntimeException ex)
            {
                await Assert
                    .That(
                        ex.Message != null
                            && ex.Message.Contains(
                                "attempt to access instance member",
                                StringComparison.Ordinal
                            )
                    )
                    .IsTrue();
            }
        }

        private static string FormatUnchecked(string format, object[] args)
        {
            ArgumentNullException.ThrowIfNull(format);

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
#pragma warning restore CA2007
