namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class OverloadedMethodMemberDescriptorTests
    {
        [OneTimeSetUp]
        public void RegisterUserData()
        {
            if (!UserData.IsTypeRegistered<MethodMemberDescriptorHost>())
            {
                UserData.RegisterType<MethodMemberDescriptorHost>();
            }

            if (!UserData.IsTypeRegistered<OverloadedMethodHost>())
            {
                UserData.RegisterType<OverloadedMethodHost>();
            }

            if (!UserData.IsTypeRegistered<string[]>())
            {
                UserData.RegisterType<string[]>();
            }
        }

        [Test]
        public void ExecuteCachesResolvedOverloadForMatchingUserDataArguments()
        {
            MethodInfo userDataOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeHost),
                typeof(MethodMemberDescriptorHost)
            );
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(
                userDataOverload,
                numberOverload
            );

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            MethodMemberDescriptorHost argument = new();
            argument.SetName("cached");

            CallbackArguments args = TestHelpers.CreateArguments(UserData.Create(argument));
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            DynValue first = callback(context, args);
            DynValue second = callback(context, args);

            Assert.Multiple(() =>
            {
                Assert.That(first.String, Is.EqualTo("host:cached"));
                Assert.That(second.String, Is.EqualTo("host:cached"));
            });
        }

        [Test]
        public void ExecuteUsesExtensionMethodsSnapshotWhenInstanceOverloadMissing()
        {
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            MethodInfo extensionMethod = GetExtensionMethod(
                typeof(OverloadedMethodHostExtensions),
                nameof(OverloadedMethodHostExtensions.AppendSuffix),
                typeof(OverloadedMethodHost),
                typeof(string)
            );

            descriptor.SetExtensionMethodsSnapshot(
                UserData.GetExtensionMethodsChangeVersion(),
                new List<IOverloadableMemberDescriptor>
                {
                    new MethodMemberDescriptor(extensionMethod, InteropAccessMode.Reflection),
                }
            );

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new() { Label = "ext" };
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            DynValue result = callback(context, TestHelpers.CreateArguments(DynValue.NewString("!")));

            Assert.That(result.String, Is.EqualTo("ext!"));
        }

        [Test]
        public void ExecuteThrowsWhenExtensionMethodsAreIgnored()
        {
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            MethodInfo extensionMethod = GetExtensionMethod(
                typeof(OverloadedMethodHostExtensions),
                nameof(OverloadedMethodHostExtensions.AppendSuffix),
                typeof(OverloadedMethodHost),
                typeof(string)
            );

            descriptor.SetExtensionMethodsSnapshot(
                UserData.GetExtensionMethodsChangeVersion(),
                new List<IOverloadableMemberDescriptor>
                {
                    new MethodMemberDescriptor(extensionMethod, InteropAccessMode.Reflection),
                }
            );

            descriptor.IgnoreExtensionMethods = true;

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new() { Label = "ext" };
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            Assert.That(
                () => callback(context, TestHelpers.CreateArguments(DynValue.NewString("!"))),
                Throws.TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("function call doesn't match any overload")
            );
        }

        [Test]
        public void ExecutePrefersExactMatchOverVarArgsWhenPenaltiesApply()
        {
            ParameterDescriptor separator = new("separator", typeof(string));
            ParameterDescriptor values = new("values", typeof(string[]), isVarArgs: true);
            RecordingOverloadDescriptor varArgs = new(
                "JoinMany",
                MemberDescriptorAccess.CanExecute,
                parameters: new[] { separator, values },
                resultFactory: _ => DynValue.NewString("varargs"),
                varArgsArrayType: typeof(string[]),
                varArgsElementType: typeof(string)
            );
            RecordingOverloadDescriptor single = new(
                "JoinSingle",
                MemberDescriptorAccess.CanExecute,
                parameters: new[] { new ParameterDescriptor("value", typeof(string)) },
                resultFactory: _ => DynValue.NewString("single")
            );
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "Join",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { varArgs, single }
                );

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                null
            );

            DynValue result = callback(
                context,
                TestHelpers.CreateArguments(
                    DynValue.NewString("-"),
                    DynValue.NewString("a"),
                    DynValue.NewString("b"),
                    DynValue.NewString("c")
                )
            );

            Assert.That(result.String, Is.EqualTo("single"));
        }

        [Test]
        public void VarArgsUserDataArraysAreTreatedAsExactMatches()
        {
            MethodInfo varArgOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.JoinMany),
                typeof(string),
                typeof(string[])
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(varArgOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            DynValue userDataArray = UserData.Create(new[] { "cached" });
            DynValue result = callback(
                context,
                TestHelpers.CreateArguments(DynValue.NewString("|"), userDataArray)
            );

            Assert.That(result.String, Is.EqualTo("cached"));
        }

        [Test]
        public void VarArgsEmptyArgumentsStillEvaluate()
        {
            MethodInfo varArgOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.JoinMany),
                typeof(string),
                typeof(string[])
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(varArgOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            DynValue result = callback(
                context,
                TestHelpers.CreateArguments(DynValue.NewString("-"))
            );

            Assert.That(result.String, Is.EqualTo(string.Empty));
        }

        [Test]
        public void VarArgsUserDataArrayMatchesDescriptorMetadata()
        {
            ParameterDescriptor[] parameters =
                new[] { new ParameterDescriptor("values", typeof(string[]), isVarArgs: true) };
            RecordingOverloadDescriptor stub = new(
                "VarArgsStub",
                MemberDescriptorAccess.CanExecute,
                parameters: parameters,
                resultFactory: _ => DynValue.NewString("vararg"),
                varArgsArrayType: typeof(string[]),
                varArgsElementType: typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "VarArgsStub",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { stub }
                );

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                null
            );

            DynValue userDataArray = UserData.Create(new[] { "wrapped" });
            DynValue result = callback(context, TestHelpers.CreateArguments(userDataArray));

            Assert.That(result.String, Is.EqualTo("vararg"));
        }

        [Test]
        public void VarArgsWithoutAdditionalArgumentsApplyEmptyWeights()
        {
            ParameterDescriptor[] parameters =
                new[] { new ParameterDescriptor("values", typeof(string[]), isVarArgs: true) };
            RecordingOverloadDescriptor stub = new(
                "VarArgsStub",
                MemberDescriptorAccess.CanExecute,
                parameters: parameters,
                resultFactory: _ => DynValue.NewString("empty"),
                varArgsArrayType: typeof(string[]),
                varArgsElementType: typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "VarArgsStub",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { stub }
                );

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                null
            );

            DynValue result = callback(context, TestHelpers.CreateArguments());

            Assert.That(result.String, Is.EqualTo("empty"));
        }

        [Test]
        public void CacheOverflowReinitializesCacheArray()
        {
            MethodInfo method = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(method);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();

            FieldInfo cacheField = typeof(OverloadedMethodMemberDescriptor).GetField(
                "_cache",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Array zeroCache = Array.CreateInstance(cacheField.FieldType.GetElementType(), 0);
            cacheField.SetValue(descriptor, zeroCache);

            DynValue result = descriptor
                .GetCallback(script, host)(
                    context,
                    TestHelpers.CreateArguments(DynValue.NewNumber(1))
                );

            Assert.That(result.String, Is.EqualTo("num:1"));
        }

        [Test]
        public void CalcScoreTreatsUserDataArrayAsExactMatch()
        {
            ParameterDescriptor[] parameters =
                new[] { new ParameterDescriptor("values", typeof(string[]), isVarArgs: true) };
            RecordingOverloadDescriptor stub = new(
                "VarArgsStub",
                MemberDescriptorAccess.CanExecute,
                parameters: parameters,
                resultFactory: _ => DynValue.NewString("exact"),
                varArgsArrayType: typeof(string[]),
                varArgsElementType: typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "VarArgsStub",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { stub }
                );

            DynValue userDataArray = UserData.Create(new[] { "lua" });
            int score = OverloadedMethodMemberDescriptorTestUtilities.InvokeCalcScore(
                descriptor,
                stub,
                new List<DynValue> { userDataArray }
            );

            Assert.That(score, Is.GreaterThan(0));
        }

        [Test]
        public void CalcScoreHandlesEmptyVarArgsSet()
        {
            ParameterDescriptor[] parameters =
                new[] { new ParameterDescriptor("values", typeof(string[]), isVarArgs: true) };
            RecordingOverloadDescriptor stub = new(
                "VarArgsStub",
                MemberDescriptorAccess.CanExecute,
                parameters: parameters,
                resultFactory: _ => DynValue.NewString("empty"),
                varArgsArrayType: typeof(string[]),
                varArgsElementType: typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "VarArgsStub",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { stub }
                );

            int score = OverloadedMethodMemberDescriptorTestUtilities.InvokeCalcScore(
                descriptor,
                stub,
                new List<DynValue>()
            );

            Assert.That(score, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ExecuteRefreshesExtensionSnapshotWhenOutOfDate()
        {
            MethodInfo joinSingle = GetInstanceMethod(
                nameof(OverloadedMethodHost.JoinSingle),
                typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(joinSingle);

            // Register a fresh extension type so the descriptor sees a new version.
            UserData.RegisterExtensionType(typeof(OverloadedMethodHostExtensionsAlt));

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new() { Label = "snap" };
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            DynValue result = callback(
                context,
                TestHelpers.CreateArguments(DynValue.NewString("!"), DynValue.True)
            );

            Assert.That(result.String, Is.EqualTo("snap!"));
        }

        [Test]
        public void CachedEntriesAreIgnoredWhenInvocationSwitchesToStatic()
        {
            MethodInfo instance = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            MethodInfo @static = GetStaticMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(string),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(instance, @static);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new() { Label = "cache" };

            Func<ScriptExecutionContext, CallbackArguments, DynValue> instanceCallback = descriptor.GetCallback(
                script,
                host
            );
            DynValue instanceResult = instanceCallback(
                context,
                TestHelpers.CreateArguments(DynValue.NewNumber(1.0))
            );
            Assert.That(instanceResult.String, Is.EqualTo("num:1"));

            Func<ScriptExecutionContext, CallbackArguments, DynValue> staticCallback = descriptor.GetCallback(
                script,
                null
            );
            DynValue staticResult = staticCallback(
                context,
                TestHelpers.CreateArguments(
                    DynValue.NewString("static"),
                    DynValue.NewNumber(2.5)
                )
            );

            Assert.That(staticResult.String, Is.EqualTo("static:2.5"));
        }

        [Test]
        public void EnumerableConstructorAddsOverloadsAndExposesCount()
        {
            RecordingOverloadDescriptor first = new(
                "Stub",
                MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead,
                resultFactory: _ => DynValue.NewString("first"),
                sortKey: "0"
            );
            RecordingOverloadDescriptor second = new(
                "Stub",
                MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead,
                resultFactory: _ => DynValue.NewString("second"),
                sortKey: "1"
            );

            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "Stub",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { first, second }
                );

            Assert.That(descriptor.OverloadCount, Is.EqualTo(2));

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue result = descriptor
                .GetCallback(script, null)(context, TestHelpers.CreateArguments());

            Assert.That(result.String, Is.EqualTo("first"));
        }

        [Test]
        public void GetCallbackFunctionReturnsNamedDelegate()
        {
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();

            CallbackFunction function = descriptor.GetCallbackFunction(script, host);
            DynValue result = function.Invoke(context, new[] { DynValue.NewNumber(12) });

            Assert.Multiple(() =>
            {
                Assert.That(function.Name, Is.EqualTo("DescribeNumber"));
                Assert.That(result.String, Is.EqualTo("num:12"));
            });
        }

        [Test]
        public void GetValueReturnsCallbackDynValue()
        {
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();

            DynValue value = descriptor.GetValue(script, host);

            Assert.Multiple(() =>
            {
                Assert.That(value.Type, Is.EqualTo(DataType.ClrFunction));
                DynValue result = value.Callback.Invoke(
                    context,
                    new[] { DynValue.NewNumber(3) }
                );
                Assert.That(result.String, Is.EqualTo("num:3"));
            });
        }

        [Test]
        public void SetValueThrowsWhenWriteIsAttempted()
        {
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            Script script = new Script();
            OverloadedMethodHost host = new();

            Assert.That(
                () => descriptor.SetValue(script, host, DynValue.NewNumber(1)),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void PrepareForWiringSerializesOverloadMetadata()
        {
            MethodInfo varArgOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.JoinMany),
                typeof(string),
                typeof(string[])
            );
            MethodInfo singleOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.JoinSingle),
                typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(
                varArgOverload,
                singleOverload
            );

            Script script = new Script();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            Assert.Multiple(() =>
            {
                Assert.That(table.Get("class").String, Does.Contain("OverloadedMethodMemberDescriptor"));
                Assert.That(table.Get("name").String, Is.EqualTo("JoinMany"));
                Assert.That(table.Get("overloads").Table.Length, Is.EqualTo(2));
            });
        }

        [Test]
        public void OptimizeDelegatesToChildDescriptors()
        {
            RecordingOverloadDescriptor child = new(
                "Optimizable",
                MemberDescriptorAccess.CanExecute,
                resultFactory: _ => DynValue.NewString("optimized")
            );
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "Optimizable",
                    typeof(OverloadedMethodHost),
                    new IOverloadableMemberDescriptor[] { child }
                );

            ((IOptimizableDescriptor)descriptor).Optimize();

            Assert.That(child.OptimizeCalled, Is.True);
        }

        [Test]
        public void ExecuteThrowsWhenNoOverloadMatchesArguments()
        {
            MethodInfo numberOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            Assert.That(
                () => callback(context, TestHelpers.CreateArguments()),
                Throws.TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("function call doesn't match any overload")
            );
        }

        [Test]
        public void SingleOverloadFastPathExecutesWithoutSorting()
        {
            MethodInfo singleOverload = GetInstanceMethod(
                nameof(OverloadedMethodHost.JoinSingle),
                typeof(string)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(singleOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback = descriptor.GetCallback(
                script,
                host
            );

            DynValue result = callback(context, TestHelpers.CreateArguments(DynValue.NewString("value")));

            Assert.That(result.String, Is.EqualTo("value"));
        }

        [Test]
        public void PrepareForWiringCapturesUnsupportedDescriptors()
        {
            NonWireableOverloadDescriptor nonWireable = new("unsupported");
            OverloadedMethodMemberDescriptor descriptor =
                new OverloadedMethodMemberDescriptor(
                    "unsupported",
                    typeof(OverloadedMethodHost),
                    new[] { nonWireable }
                );

            Script script = new Script();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            DynValue entry = table.Get("overloads").Table.Get(1);
            Assert.Multiple(() =>
            {
                Assert.That(entry.Type, Is.EqualTo(DataType.String));
                Assert.That(entry.String, Does.Contain("unsupported"));
            });
        }

        [Test]
        public void IsStaticReflectsContainedOverloads()
        {
            MethodInfo instanceMethod = GetInstanceMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(double)
            );
            MethodInfo staticMethod = GetStaticMethod(
                nameof(OverloadedMethodHost.DescribeNumber),
                typeof(string),
                typeof(double)
            );

            OverloadedMethodMemberDescriptor instanceDescriptor = CreateDescriptor(instanceMethod);
            OverloadedMethodMemberDescriptor staticDescriptor = CreateDescriptor(staticMethod);

            Assert.Multiple(() =>
            {
                Assert.That(instanceDescriptor.IsStatic, Is.False);
                Assert.That(staticDescriptor.IsStatic, Is.True);
            });
        }

        private static MethodInfo GetInstanceMethod(string name, params Type[] parameterTypes)
        {
            return GetMethod(
                typeof(OverloadedMethodHost),
                BindingFlags.Instance | BindingFlags.Public,
                name,
                parameterTypes
            );
        }

        private static MethodInfo GetStaticMethod(string name, params Type[] parameterTypes)
        {
            return GetMethod(
                typeof(OverloadedMethodHost),
                BindingFlags.Static | BindingFlags.Public,
                name,
                parameterTypes
            );
        }

        private static MethodInfo GetExtensionMethod(
            Type declaringType,
            string name,
            params Type[] parameterTypes
        )
        {
            return GetMethod(declaringType, BindingFlags.Static | BindingFlags.Public, name, parameterTypes);
        }

        private static MethodInfo GetMethod(
            Type declaringType,
            BindingFlags bindingFlags,
            string name,
            params Type[] parameterTypes
        )
        {
            Type[] normalizedParameters =
                parameterTypes is { Length: > 0 } ? parameterTypes : Type.EmptyTypes;
            MethodInfo method = declaringType.GetMethod(
                name,
                bindingFlags,
                binder: null,
                types: normalizedParameters,
                modifiers: null
            );

            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Method '{name}' was not found on {declaringType.FullName}."
                );
            }

            return method;
        }

        private static OverloadedMethodMemberDescriptor CreateDescriptor(params MethodInfo[] overloads)
        {
            OverloadedMethodMemberDescriptor descriptor = new(
                overloads[0].Name,
                overloads[0].DeclaringType
            );

            foreach (MethodInfo method in overloads)
            {
                descriptor.AddOverload(new MethodMemberDescriptor(method, InteropAccessMode.Reflection));
            }

            return descriptor;
        }
    }

    internal sealed class OverloadedMethodHost
    {
        public string Label { get; set; } = "host";

        public string DescribeHost(MethodMemberDescriptorHost other)
        {
            return $"host:{other?.LastName ?? "null"}";
        }

        public string DescribeNumber(double value)
        {
            return $"num:{value}";
        }

        public static string DescribeNumber(string prefix, double value)
        {
            return $"{prefix}:{value}";
        }

        public string JoinSingle(string value)
        {
            return value;
        }

        public string JoinMany(string separator, params string[] values)
        {
            return string.Join(separator, values);
        }
    }

    internal static class OverloadedMethodHostExtensions
    {
        public static string AppendSuffix(this OverloadedMethodHost host, string suffix)
        {
            return host.Label + suffix;
        }
    }

    internal static class OverloadedMethodHostExtensionsAlt
    {
        public static string JoinSingle(
            this OverloadedMethodHost host,
            string suffix,
            bool includeLabel
        )
        {
            return includeLabel ? host.Label + suffix : suffix.ToUpperInvariant();
        }
    }

    internal static class OverloadedMethodMemberDescriptorTestUtilities
    {
        public static int InvokeCalcScore(
            OverloadedMethodMemberDescriptor descriptor,
            IOverloadableMemberDescriptor overload,
            IList<DynValue> dynValues
        )
        {
            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = new CallbackArguments(dynValues, false);
            MethodInfo calcScore = typeof(OverloadedMethodMemberDescriptor).GetMethod(
                "CalcScoreForOverload",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            return (int)
                calcScore.Invoke(descriptor, new object[] { context, args, overload, false });
        }
    }

    internal sealed class RecordingOverloadDescriptor
        : IOverloadableMemberDescriptor,
            IOptimizableDescriptor,
            IWireableDescriptor
    {
        private readonly MemberDescriptorAccess access;
        private readonly bool isStatic;
        private readonly ParameterDescriptor[] parameters;
        private readonly string sortDiscriminant;
        private readonly System.Func<CallbackArguments, DynValue> executor;
        private readonly Type varArgsArrayType;
        private readonly Type varArgsElementType;
        private readonly Type extensionMethodType;

        public RecordingOverloadDescriptor(
            string name,
            MemberDescriptorAccess access,
            bool isStatic = true,
            string sortKey = "0",
            ParameterDescriptor[] parameters = null,
            System.Func<CallbackArguments, DynValue> resultFactory = null,
            Type varArgsArrayType = null,
            Type varArgsElementType = null,
            Type extensionMethodType = null
        )
        {
            Name = name;
            this.access = access;
            this.isStatic = isStatic;
            this.parameters = parameters ?? System.Array.Empty<ParameterDescriptor>();
            sortDiscriminant = sortKey;
            executor = resultFactory ?? (_ => DynValue.Void);
            this.varArgsArrayType = varArgsArrayType;
            this.varArgsElementType = varArgsElementType;
            this.extensionMethodType = extensionMethodType;
        }

        public bool OptimizeCalled { get; private set; }

        public bool IsStatic => isStatic;

        public string Name { get; }

        public MemberDescriptorAccess MemberAccess => access;

        public Type ExtensionMethodType => extensionMethodType;

        public IReadOnlyList<ParameterDescriptor> Parameters => parameters;

        public Type VarArgsArrayType => varArgsArrayType;

        public Type VarArgsElementType => varArgsElementType;

        public string SortDiscriminant => sortDiscriminant;

        public DynValue Execute(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        )
        {
            return executor(args);
        }

        public DynValue GetValue(Script script, object obj)
        {
            return DynValue.NewCallback((ctx, arguments) => executor(arguments));
        }

        public void SetValue(Script script, object obj, DynValue value)
        {
            throw new ScriptRuntimeException("not writable");
        }

        public void Optimize()
        {
            OptimizeCalled = true;
        }

        public void PrepareForWiring(Table t)
        {
            t.Set("name", DynValue.NewString(Name));
        }
    }

    internal sealed class NonWireableOverloadDescriptor : IOverloadableMemberDescriptor
    {
        private readonly string name;

        public NonWireableOverloadDescriptor(string name)
        {
            this.name = name;
        }

        public bool IsStatic => true;

        public string Name => name;

        public MemberDescriptorAccess MemberAccess =>
            MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead;

        public Type ExtensionMethodType => null;

        public IReadOnlyList<ParameterDescriptor> Parameters { get; } =
            System.Array.Empty<ParameterDescriptor>();

        public Type VarArgsArrayType => null;

        public Type VarArgsElementType => null;

        public string SortDiscriminant => name;

        public DynValue Execute(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        )
        {
            return DynValue.Void;
        }

        public DynValue GetValue(Script script, object obj)
        {
            return DynValue.Nil;
        }

        public void SetValue(Script script, object obj, DynValue value)
        {
            throw new ScriptRuntimeException("unsupported");
        }
    }
}
