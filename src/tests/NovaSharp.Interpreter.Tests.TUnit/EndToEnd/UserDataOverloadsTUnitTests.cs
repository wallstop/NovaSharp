namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;
#if !DOTNET_CORE
    using System.Linq;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
#endif

    internal static class OverloadsExtMethods
    {
        internal static string Method1(
            this UserDataOverloadsTUnitTests.OverloadsTestClass obj,
            string x,
            bool b
        )
        {
            return "X" + obj.Method1();
        }

        internal static string Method3(this UserDataOverloadsTUnitTests.OverloadsTestClass obj)
        {
            obj.Method1();
            return "X3";
        }
    }

    internal static class OverloadsExtMethods2
    {
        internal static string MethodXxx(
            this UserDataOverloadsTUnitTests.OverloadsTestClass obj,
            string x,
            bool b
        )
        {
            return "X!";
        }
    }

    [UserDataIsolation]
    public sealed class UserDataOverloadsTUnitTests
    {
        private int _helperCallCount;

        internal sealed class OverloadsTestClass
        {
            private int _callCounter;
            private string _lastFormat = string.Empty;

            public string MethodV(string fmt, params object[] args)
            {
                _lastFormat = fmt ?? string.Empty;
                int argumentCount = 0;
                if (args != null)
                {
                    argumentCount = args.Length;
                }

                _callCounter += argumentCount;
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
                int valueLength = 0;
                if (y != null)
                {
                    valueLength = y.Length;
                }

                _callCounter += valueLength;
                return "v";
            }

            public string Method2(string x, ref string y)
            {
                _lastFormat = x ?? string.Empty;
                int valueLength = 0;
                if (y != null)
                {
                    valueLength = y.Length;
                }

                _callCounter += valueLength;
                return "r";
            }

            public string Method2(string x, ref string y, int z)
            {
                _lastFormat = x ?? string.Empty;
                _callCounter += z;
                return "R";
            }
        }

        private static async Task RunTestOverloadAsync(
            string code,
            string expected,
            bool tupleExpected = false
        )
        {
            Script script = new();
            OverloadsTestClass obj = new();

            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<OverloadsTestClass>(ensureUnregistered: true);
            registrationScope.RegisterType<OverloadsTestClass>();

            script.Globals.Set("s", UserData.CreateStatic<OverloadsTestClass>());
            script.Globals.Set("o", UserData.Create(obj));

            DynValue result = script.DoString("return " + code);

            if (tupleExpected)
            {
                await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
                result = result.Tuple[0];
            }

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public Task InteropOutParamInOverloadResolution()
        {
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track<
                Dictionary<int, int>
            >(ensureUnregistered: true);

            UserData.RegisterType<Dictionary<int, int>>();
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            Script lua = new()
            {
                Globals = { ["DictionaryIntInt"] = typeof(Dictionary<int, int>) },
            };

            string script =
                @"local dict = DictionaryIntInt.__new(); local res, v = dict.TryGetValue(0)";
            lua.DoString(script);
            lua.DoString(script);

            return Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsVarargs1()
        {
            return RunTestOverloadAsync("o:methodV('{0}-{1}', 15, true)", "exact:15-True");
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsVarargs2()
        {
            return RunTestOverloadAsync(
                "o:methodV('{0}-{1}-{2}', 15, true, false)",
                "varargs:15-True-False"
            );
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsByRef()
        {
            return RunTestOverloadAsync("o:method2('x', 'y')", "v");
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsByRef2()
        {
            return RunTestOverloadAsync("o:method2('x', 'y', 5)", "R", true);
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsNoParams()
        {
            return RunTestOverloadAsync("o:method1()", "1");
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsNumDowncast()
        {
            return RunTestOverloadAsync("o:method1(5)", "3");
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsNilSelectsNonOptional()
        {
            return RunTestOverloadAsync("o:method1(5, nil)", "4");
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsFullDecl()
        {
            return RunTestOverloadAsync("o:method1(5, nil, 0)", "5");
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsStatic1()
        {
            return RunTestOverloadAsync("s:method1(true)", "s");
        }

        [global::TUnit.Core.Test]
        public async Task InteropOverloadsExtMethods()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            await RunTestOverloadAsync("o:method1('xx', true)", "X1").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method3()", "X3").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropOverloadsTwiceExtMethods1()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            await RunTestOverloadAsync("o:method1('xx', true)", "X1").ConfigureAwait(false);

            UserData.RegisterExtensionType(typeof(OverloadsExtMethods2));

            await RunTestOverloadAsync("o:methodXXX('xx', true)", "X!").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropOverloadsTwiceExtMethods2()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods2));

            await RunTestOverloadAsync("o:method1('xx', true)", "X1").ConfigureAwait(false);
            await RunTestOverloadAsync("o:methodXXX('xx', true)", "X!").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public Task InteropOverloadsExtMethods2()
        {
            UserData.RegisterExtensionType(typeof(OverloadsExtMethods));

            Assert.Throws<ScriptRuntimeException>(() =>
                RunTestOverloadAsync("s:method3()", "X3").GetAwaiter().GetResult()
            );

            return Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public async Task InteropOverloadsStatic2()
        {
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);

            Assert.Throws<ScriptRuntimeException>(() =>
                RunTestOverloadAsync("s:method1(5)", "s").GetAwaiter().GetResult()
            );
        }

        [global::TUnit.Core.Test]
        public async Task InteropOverloadsCache1()
        {
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropOverloadsCache2()
        {
            await RunTestOverloadAsync("o:method1()", "1").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, nil)", "4").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, nil, 0)", "5").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("s:method1(true)", "s").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, nil, 0)", "5").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, 'x')", "4").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, 'x', 0)", "5").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, nil, 0)", "5").ConfigureAwait(false);
            await RunTestOverloadAsync("s:method1(true)", "s").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, 5)", "4").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, nil, 0)", "5").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("s:method1(true)", "s").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5)", "3").ConfigureAwait(false);
            await RunTestOverloadAsync("o:method1(5, 5, 0)", "5").ConfigureAwait(false);
            await RunTestOverloadAsync("s:method1(true)", "s").ConfigureAwait(false);
        }

        private int Method1()
        {
            _helperCallCount++;
            return 1;
        }

        private int Method1(int a)
        {
            _helperCallCount += a;
            return 5 + a;
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

#if !DOTNET_CORE
        [global::TUnit.Core.Test]
        public async Task OverloadTestWithoutObjects()
        {
            Script script = new();

            OverloadedMethodMemberDescriptor descriptor = new("Method1", GetType());

            foreach (
                var method in Framework
                    .Do.GetMethods(GetType())
                    .Where(mi => mi.Name == "Method1" && mi.IsPrivate && !mi.IsStatic)
            )
            {
                descriptor.AddOverload(new MethodMemberDescriptor(method));
            }

            DynValue callback = DynValue.NewCallback(descriptor.GetCallbackFunction(script, this));
            script.Globals.Set("func", callback);

            DynValue result = script.DoString("return func(), func(17)");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Type)
                .IsEqualTo(DataType.Number)
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Type)
                .IsEqualTo(DataType.Number)
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(22).ConfigureAwait(false);
        }
#endif
    }
}
