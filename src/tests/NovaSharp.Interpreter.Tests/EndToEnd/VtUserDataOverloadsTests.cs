namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Globalization;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    internal static class VtOverloadsExtMethods
    {
        internal static string Method1(
            this VtUserDataOverloadsTests.OverloadsTestClass obj,
            string x,
            bool b
        )
        {
            return "X" + obj.Method1();
        }

        internal static string Method3(this VtUserDataOverloadsTests.OverloadsTestClass obj)
        {
            obj.Method1();
            return "X3";
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    [UserDataIsolation]
    public class VtUserDataOverloadsTests
    {
        internal struct OverloadsTestClass
        {
            private int _callCounter;

            private void RecordCall(int delta = 1)
            {
                _callCounter = unchecked(_callCounter + Math.Max(1, delta));
            }

            public static void UnCalled()
            {
                OverloadsTestClass otc = new();
                otc.Method1();
                Method1(false);
            }

            public string MethodV(string fmt, params object[] args)
            {
                RecordCall(args?.Length ?? 1);
                return "varargs:" + FormatUnchecked(fmt, args);
            }

            public string MethodV(string fmt, int a, bool b)
            {
                RecordCall(a);
                return "exact:" + string.Format(CultureInfo.InvariantCulture, fmt, a, b);
            }

            public string Method1()
            {
                RecordCall();
                return "1";
            }

            public static string Method1(bool b)
            {
                return "s";
            }

            public string Method1(int a)
            {
                RecordCall(a);
                return "2";
            }

            public string Method1(double d)
            {
                RecordCall((int)Math.Round(d));
                return "3";
            }

            public string Method1(double d, string x = null)
            {
                RecordCall();
                return "4";
            }

            public string Method1(double d, string x, int y = 5)
            {
                RecordCall(y);
                return "5";
            }

            public string Method2(string x, string y)
            {
                RecordCall(x?.Length ?? 1);
                return "v";
            }

            public string Method2(string x, ref string y)
            {
                RecordCall(x?.Length ?? 1);
                return "r";
            }

            public string Method2(string x, ref string y, int z)
            {
                RecordCall(z);
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
        public void VInteropOverloadsVarargs1()
        {
            RunTestOverload("o:methodV('{0}-{1}', 15, true)", "exact:15-True");
        }

        [Test]
        public void VInteropOverloadsVarargs2()
        {
            RunTestOverload("o:methodV('{0}-{1}-{2}', 15, true, false)", "varargs:15-True-False");
        }

        [Test]
        public void VInteropOverloadsByRef()
        {
            RunTestOverload("o:method2('x', 'y')", "v");
        }

        [Test]
        public void VInteropOverloadsByRef2()
        {
            RunTestOverload("o:method2('x', 'y', 5)", "R", true);
        }

        [Test]
        public void VInteropOverloadsNoParams()
        {
            RunTestOverload("o:method1()", "1");
        }

        [Test]
        public void VInteropOverloadsNumDowncast()
        {
            RunTestOverload("o:method1(5)", "3");
        }

        [Test]
        public void VInteropOverloadsNilSelectsNonOptional()
        {
            RunTestOverload("o:method1(5, nil)", "4");
        }

        [Test]
        public void VInteropOverloadsFullDecl()
        {
            RunTestOverload("o:method1(5, nil, 0)", "5");
        }

        [Test]
        public void VInteropOverloadsStatic1()
        {
            RunTestOverload("s:method1(true)", "s");
        }

        [Test]
        public void VInteropOverloadsExtMethods()
        {
            UserData.RegisterExtensionType(typeof(VtOverloadsExtMethods));

            RunTestOverload("o:method1('xx', true)", "X1");
            RunTestOverload("o:method3()", "X3");
        }

        [Test]
        public void VInteropOverloadsExtMethods2()
        {
            UserData.RegisterExtensionType(typeof(VtOverloadsExtMethods));
            Assert.Throws<ScriptRuntimeException>(() => RunTestOverload("s:method3()", "X3"));
        }

        [Test]
        public void VInteropOverloadsStatic2()
        {
            // pollute cache
            RunTestOverload("o:method1(5)", "3");
            // exec non static on static
            Assert.Throws<ScriptRuntimeException>(() => RunTestOverload("s:method1(5)", "s"));
        }

        [Test]
        public void VInteropOverloadsCache1()
        {
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5)", "3");
            RunTestOverload("o:method1(5)", "3");
        }

        [Test]
        public void VInteropOverloadsCache2()
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
