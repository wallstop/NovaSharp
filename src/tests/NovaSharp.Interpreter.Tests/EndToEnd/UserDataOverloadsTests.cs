namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    internal static class OverloadsExtMethods
    {
        internal static string Method1(
            this UserDataOverloadsTests.OverloadsTestClass obj,
            string x,
            bool b
        )
        {
            return "X" + obj.Method1();
        }

        internal static string Method3(this UserDataOverloadsTests.OverloadsTestClass obj)
        {
            obj.Method1();
            return "X3";
        }
    }

    internal static class OverloadsExtMethods2
    {
        internal static string MethodXxx(
            this UserDataOverloadsTests.OverloadsTestClass obj,
            string x,
            bool b
        )
        {
            return "X!";
        }
    }

    [TestFixture]
    public class UserDataOverloadsTests
    {
        internal sealed class OverloadsTestClass
        {
            private int _callCounter;
            private string _lastFormat = string.Empty;

            public string MethodV(string fmt, params object[] args)
            {
                _lastFormat = fmt ?? string.Empty;
                _callCounter += args?.Length ?? 0;
                return "varargs:" + FormatUnchecked(fmt, args);
            }

            public string MethodV(string fmt, int a, bool b)
            {
                _lastFormat = fmt ?? string.Empty;
                _callCounter += a;
                return "exact:" + string.Format(CultureInfo.InvariantCulture, fmt, a, b);
            }

            public string Method1()
            {
                _callCounter++;
                return "1";
            }

            public static string Method1(bool b)
            {
                return "s";
            }

            public string Method1(int a)
            {
                _callCounter += a;
                return "2";
            }

            public string Method1(double d)
            {
                _callCounter += (int)d;
                return "3";
            }

            public string Method1(double d, string x = null)
            {
                _callCounter += (int)d;
                _lastFormat = x ?? string.Empty;
                return "4";
            }

            public string Method1(double d, string x, int y = 5)
            {
                _callCounter += y;
                _lastFormat = x ?? string.Empty;
                return "5";
            }

            public string Method2(string x, string y)
            {
                _lastFormat = x ?? string.Empty;
                _callCounter += y?.Length ?? 0;
                return "v";
            }

            public string Method2(string x, ref string y)
            {
                _lastFormat = x ?? string.Empty;
                _callCounter += y?.Length ?? 0;
                return "r";
            }

            public string Method2(string x, ref string y, int z)
            {
                _lastFormat = x ?? string.Empty;
                _callCounter += z;
                return "R";
            }
        }

        private static void RunTestOverload(
            string code,
            string expected,
            bool tupleExpected = false
        )
        {
            Script s = new();

            OverloadsTestClass obj = new();

            UserData.RegisterType<OverloadsTestClass>();

            s.Globals.Set("s", UserData.CreateStatic<OverloadsTestClass>());
            s.Globals.Set("o", UserData.Create(obj));

            DynValue v = s.DoString("return " + code);

            if (tupleExpected)
            {
                Assert.That(v.Type, Is.EqualTo(DataType.Tuple));
                v = v.Tuple[0];
            }

            Assert.That(v.Type, Is.EqualTo(DataType.String));
            Assert.That(v.String, Is.EqualTo(expected));
        }

        [Test]
        public void InteropOutParamInOverloadResolution()
        {
            UserData.RegisterType<Dictionary<int, int>>();
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            try
            {
                Script lua = new()
                {
                    Globals = { ["DictionaryIntInt"] = typeof(Dictionary<int, int>) },
                };

                string script =
                    @"local dict = DictionaryIntInt.__new(); local res, v = dict.TryGetValue(0)";
                lua.DoString(script);
                lua.DoString(script);
            }
            finally
            {
                UserData.UnregisterType<Dictionary<int, int>>();
            }
        }

        [Test]
        public void InteropOverloadsVarargs1()
        {
            RunTestOverload("o:methodV('{0}-{1}', 15, true)", "exact:15-True");
        }

        [Test]
        public void InteropOverloadsVarargs2()
        {
            RunTestOverload("o:methodV('{0}-{1}-{2}', 15, true, false)", "varargs:15-True-False");
        }

        [Test]
        public void InteropOverloadsByRef()
        {
            RunTestOverload("o:method2('x', 'y')", "v");
        }

        [Test]
        public void InteropOverloadsByRef2()
        {
            RunTestOverload("o:method2('x', 'y', 5)", "R", true);
        }

        [Test]
        public void InteropOverloadsNoParams()
        {
            RunTestOverload("o:method1()", "1");
        }

        [Test]
        public void InteropOverloadsNumDowncast()
        {
            RunTestOverload("o:method1(5)", "3");
        }

        [Test]
        public void InteropOverloadsNilSelectsNonOptional()
        {
            RunTestOverload("o:method1(5, nil)", "4");
        }

        [Test]
        public void InteropOverloadsFullDecl()
        {
            RunTestOverload("o:method1(5, nil, 0)", "5");
        }

        [Test]
        public void InteropOverloadsStatic1()
        {
            RunTestOverload("s:method1(true)", "s");
        }

        [Test]
        public void InteropOverloadsExtMethods()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            RunTestOverload("o:method1('xx', true)", "X1");
            RunTestOverload("o:method3()", "X3");
        }

        [Test]
        public void InteropOverloadsTwiceExtMethods1()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            RunTestOverload("o:method1('xx', true)", "X1");

            UserData.RegisterExtensionType(typeof(OverloadsExtMethods2));

            RunTestOverload("o:methodXXX('xx', true)", "X!");
        }

        [Test]
        public void InteropOverloadsTwiceExtMethods2()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods2));

            RunTestOverload("o:method1('xx', true)", "X1");
            RunTestOverload("o:methodXXX('xx', true)", "X!");
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropOverloadsExtMethods2()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));
            RunTestOverload("s:method3()", "X3");
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropOverloadsStatic2()
        {
            // pollute cache
            RunTestOverload("o:method1(5)", "3");
            // exec non static on static
            RunTestOverload("s:method1(5)", "s");
        }

        [Test]
        public void InteropOverloadsCache1()
        {
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5)", "3");
        }

        [Test]
        public void InteropOverloadsCache2()
        {
            RunTestOverload("o:method1()", "1");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5, nil)", "4");
            RunTestOverload("o:method1(5, nil, 0)", "5");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("s:method1(true)", "s");
            RunTestOverload("o:method1(5, nil, 0)", "5");
            RunTestOverload("o:method1(5, 'x')", "4");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5, 'x', 0)", "5");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5, nil, 0)", "5");
            RunTestOverload("s:method1(true)", "s");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5, 5)", "4");
            RunTestOverload("o:method1(5, nil, 0)", "5");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("s:method1(true)", "s");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5, 5, 0)", "5");
            RunTestOverload("s:method1(true)", "s");
        }

        private int Method1()
        {
            return 1;
        }

        private int Method1(int a)
        {
            return 5 + a;
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

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

#if !DOTNET_CORE
        [Test]
        public void OverloadTestWithoutObjects()
        {
            Script s = new Script();

            // Create an instance of the overload resolver
            var ov = new OverloadedMethodMemberDescriptor("Method1", this.GetType());

            // Iterate over the two methods through reflection
            foreach (
                var method in Framework
                    .Do.GetMethods(this.GetType())
                    .Where(mi => mi.Name == "Method1" && mi.IsPrivate && !mi.IsStatic)
            )
            {
                ov.AddOverload(new MethodMemberDescriptor(method));
            }

            // Creates the callback over the 'this' object
            DynValue callback = DynValue.NewCallback(ov.GetCallbackFunction(s, this));
            s.Globals.Set("func", callback);

            // Execute and check the results.
            DynValue result = s.DoString("return func(), func(17)");

            Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(result.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Tuple[0].Number, Is.EqualTo(1));
            Assert.That(result.Tuple[1].Number, Is.EqualTo(22));
        }
#endif
    }
}
