namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.Units;

    [ScriptGlobalOptionsIsolation]
    [PlatformDetectorIsolation]
    public sealed class MethodMemberDescriptorTUnitTests
    {
        private static readonly object CompatibilityLock = new();

        static MethodMemberDescriptorTUnitTests()
        {
            UserData.RegisterType<MethodMemberDescriptorHost>();
            UserData.RegisterType<int[,]>();
        }

        private static void EnsureArrayUserDataRegistered()
        {
            if (!UserData.IsTypeRegistered<int[,]>())
            {
                UserData.RegisterType<int[,]>();
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteLazyOptimizedInstanceActionUsesCompiledDelegate()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = MethodMemberDescriptorHostMetadata.SetName;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewString("Lua"));

            DynValue result = descriptor.Execute(script, host, context, args);

            await Assert.That(host.LastName).IsEqualTo("Lua");
            await Assert.That(result.IsVoid()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteLazyOptimizedStaticFunctionReturnsDynValue()
        {
            MethodInfo method = MethodMemberDescriptorHostMetadata.Sum;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(4),
                DynValue.NewNumber(5)
            );

            DynValue result = descriptor.Execute(script, null, context, args);

            await Assert.That(result.Number).IsEqualTo(9d);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteReflectionModeInvokesMethodInfo()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = MethodMemberDescriptorHostMetadata.Multiply;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(6),
                DynValue.NewNumber(7)
            );

            DynValue result = descriptor.Execute(script, host, context, args);

            await Assert.That(result.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteThrowsWhenInstanceTargetMissing()
        {
            MethodInfo method = MethodMemberDescriptorHostMetadata.Multiply;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(1),
                DynValue.NewNumber(2)
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                descriptor.Execute(script, null, context, args)
            );

            await Assert.That(exception.Message).Contains("instance member");
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteReflectionModeInvokesVoidMethodThroughActionBranch()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = MethodMemberDescriptorHostMetadata.SetName;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewString("Reflection"));

            DynValue result = descriptor.Execute(script, host, context, args);

            await Assert.That(host.LastName).IsEqualTo("Reflection");
            await Assert.That(result.IsVoid()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteArrayConstructorCreatesExpectedArray()
        {
            EnsureArrayUserDataRegistered();
            ConstructorInfo ctor =
                MethodMemberDescriptorArrayMetadata.Int32TwoDimensionalConstructor;
            MethodMemberDescriptor descriptor = new(ctor, InteropAccessMode.Reflection);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            DynValue result = descriptor.Execute(script, null, context, args);

            await Assert.That(result.Type).IsEqualTo(DataType.UserData);
            int[,] array = (int[,])result.UserData.Object;
            await Assert.That(array.GetLength(0)).IsEqualTo(2);
            await Assert.That(array.GetLength(1)).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteExtensionMethodBindsInstance()
        {
            MethodMemberDescriptorHost host = new();
            host.SetName("Nova");
            MethodInfo extension = MethodMemberDescriptorTestExtensionsMetadata.Decorate;
            MethodMemberDescriptor descriptor = new(extension, InteropAccessMode.Reflection);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewString("Sharp"));

            DynValue result = descriptor.Execute(script, host, context, args);

            await Assert.That(result.String).IsEqualTo("NovaSharp");
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteOutParametersReturnTuple()
        {
            MethodMemberDescriptorHost host = new();
            MethodInfo method = MethodMemberDescriptorHostMetadata.TryDouble;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(21));

            DynValue result = descriptor.Execute(script, host, context, args);

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorDefaultAccessModeUsesGlobalDefault()
        {
            InteropAccessMode original = UserData.DefaultAccessMode;
            try
            {
                UserData.DefaultAccessMode = InteropAccessMode.Reflection;
                MethodInfo method = MethodMemberDescriptorHostMetadata.Sum;

                MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Default);

                await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Reflection);
            }
            finally
            {
                UserData.DefaultAccessMode = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithByRefParametersEnforcesReflectionAccessMode()
        {
            MethodInfo method = MethodMemberDescriptorHostMetadata.TryDouble;

            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorOnAotPlatformForcesReflectionAccessMode()
        {
            PlatformAutoDetector.TestHooks.SetRunningOnAot(true);
            MethodInfo method = MethodMemberDescriptorHostMetadata.Sum;

            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            await Assert.That(descriptor.AccessMode).IsEqualTo(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenHideMembersAccessModeRequested()
        {
            PlatformAutoDetector.TestHooks.SetRunningOnAot(false);
            MethodInfo method = MethodMemberDescriptorHostMetadata.Sum;

            ArgumentException exception = ExpectException<ArgumentException>(() =>
            {
                _ = new MethodMemberDescriptor(method, InteropAccessMode.HideMembers);
            });

            await Assert.That(exception.Message).IsEqualTo("Invalid accessMode");
        }

        [global::TUnit.Core.Test]
        public async Task TryCreateIfVisibleHonorsVisibility()
        {
            MethodInfo hidden = MethodMemberDescriptorHostMetadata.HiddenHelper;

            MethodMemberDescriptor invisible = MethodMemberDescriptor.TryCreateIfVisible(
                hidden,
                InteropAccessMode.Reflection
            );

            MethodMemberDescriptor forced = MethodMemberDescriptor.TryCreateIfVisible(
                hidden,
                InteropAccessMode.Reflection,
                forceVisibility: true
            );

            await Assert.That(invisible).IsNull();
            await Assert.That(forced).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task CheckMethodIsCompatibleRejectsOpenGenericDefinitions()
        {
            bool compatible;
            ArgumentException exception;
            lock (CompatibilityLock)
            {
                MethodInfo method = GenericMethodHostMetadata.GenericIdentity;
                compatible = MethodMemberDescriptor.CheckMethodIsCompatible(method, false);
                exception = ExpectException<ArgumentException>(() =>
                    MethodMemberDescriptor.CheckMethodIsCompatible(method, true)
                );
            }

            await Assert.That(compatible).IsFalse();
            await Assert.That(exception.Message).Contains("unresolved generic parameters");
        }

        [global::TUnit.Core.Test]
        public async Task CheckMethodIsCompatibleRejectsPointerParameters()
        {
            bool compatible;
            ArgumentException exception;
            Type pointerType = typeof(int).MakePointerType();
            DynamicMethod pointerMethod = new(
                "PointerParameter",
                typeof(void),
                new[] { pointerType },
                typeof(MethodMemberDescriptorTUnitTests).Module,
                skipVisibility: true
            );

            await Assert.That(pointerMethod.GetParameters()[0].ParameterType.IsPointer).IsTrue();
            lock (CompatibilityLock)
            {
                compatible = MethodMemberDescriptor.CheckMethodIsCompatible(pointerMethod, false);
                exception = ExpectException<ArgumentException>(() =>
                    MethodMemberDescriptor.CheckMethodIsCompatible(pointerMethod, true)
                );
            }

            await Assert.That(compatible).IsFalse();
            await Assert.That(exception.Message).Contains("pointer parameters");
        }

        [global::TUnit.Core.Test]
        public async Task CheckMethodIsCompatibleRejectsPointerReturnTypes()
        {
            bool compatible;
            ArgumentException exception;
            Type pointerType = typeof(int).MakePointerType();
            DynamicMethod pointerMethod = new(
                "ReturnPointer",
                pointerType,
                Type.EmptyTypes,
                typeof(MethodMemberDescriptorTUnitTests).Module,
                skipVisibility: true
            );

            await Assert.That(pointerMethod.ReturnType.IsPointer).IsTrue();
            lock (CompatibilityLock)
            {
                compatible = MethodMemberDescriptor.CheckMethodIsCompatible(pointerMethod, false);
                exception = ExpectException<ArgumentException>(() =>
                    MethodMemberDescriptor.CheckMethodIsCompatible(pointerMethod, true)
                );
            }

            await Assert.That(compatible).IsFalse();
            await Assert.That(exception.Message).Contains("pointer return type");
        }

        [global::TUnit.Core.Test]
        public async Task CheckMethodIsCompatibleRejectsUnboundGenericReturnTypes()
        {
            bool compatible;
            ArgumentException exception;
            Type openGeneric = typeof(System.Collections.Generic.List<>);
            DynamicMethod openReturnMethod = new(
                "ReturnOpenGeneric",
                openGeneric,
                Type.EmptyTypes,
                typeof(MethodMemberDescriptorTUnitTests).Module,
                skipVisibility: true
            );

            await Assert.That(openReturnMethod.ReturnType.IsGenericTypeDefinition).IsTrue();
            lock (CompatibilityLock)
            {
                compatible = MethodMemberDescriptor.CheckMethodIsCompatible(
                    openReturnMethod,
                    false
                );
                exception = ExpectException<ArgumentException>(() =>
                    MethodMemberDescriptor.CheckMethodIsCompatible(openReturnMethod, true)
                );
            }

            await Assert.That(compatible).IsFalse();
            await Assert.That(exception.Message).Contains("unresolved generic return type");
        }

        [global::TUnit.Core.Test]
        public async Task OptimizeThrowsWhenParametersContainByRefArguments()
        {
            MethodInfo method = MethodMemberDescriptorHostMetadata.TryDouble;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);
            MethodMemberDescriptor.TestHooks.ForceAccessMode(
                descriptor,
                InteropAccessMode.Preoptimized
            );

            InternalErrorException exception = ExpectException<InternalErrorException>(() =>
                ((IOptimizableDescriptor)descriptor).Optimize()
            );

            await Assert.That(exception.Message).Contains("Out/Ref params cannot be precompiled");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorRejectsPointerParameterMethods()
        {
            Type pointerType = typeof(int).MakePointerType();
            DynamicMethod pointerMethod = new(
                "CtorPointerParameter",
                typeof(void),
                new[] { pointerType },
                typeof(MethodMemberDescriptorTUnitTests).Module,
                skipVisibility: true
            );

            ArgumentException exception = ExpectException<ArgumentException>(() =>
            {
                _ = new MethodMemberDescriptor(pointerMethod, InteropAccessMode.Reflection);
            });

            await Assert.That(exception.Message).Contains("pointer parameters");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorRejectsPointerReturnMethods()
        {
            Type pointerType = typeof(int).MakePointerType();
            DynamicMethod pointerMethod = new(
                "CtorPointerReturn",
                pointerType,
                Type.EmptyTypes,
                typeof(MethodMemberDescriptorTUnitTests).Module,
                skipVisibility: true
            );

            ArgumentException exception = ExpectException<ArgumentException>(() =>
            {
                _ = new MethodMemberDescriptor(pointerMethod, InteropAccessMode.Reflection);
            });

            await Assert.That(exception.Message).Contains("pointer return type");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorRejectsUnboundGenericReturnMethods()
        {
            Type openGeneric = typeof(System.Collections.Generic.List<>);
            DynamicMethod openReturnMethod = new(
                "CtorOpenGenericReturn",
                openGeneric,
                Type.EmptyTypes,
                typeof(MethodMemberDescriptorTUnitTests).Module,
                skipVisibility: true
            );

            ArgumentException exception = ExpectException<ArgumentException>(() =>
            {
                _ = new MethodMemberDescriptor(openReturnMethod, InteropAccessMode.Reflection);
            });

            await Assert.That(exception.Message).Contains("unresolved generic return type");
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringPopulatesDescriptorMetadata()
        {
            MethodInfo method = MethodMemberDescriptorHostMetadata.SetName;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.LazyOptimized);

            Script script = new();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            await Assert.That(table.Get("class").String).Contains("MethodMemberDescriptor");
            await Assert.That(table.Get("name").String).IsEqualTo("SetName");
            await Assert.That(table.Get("ctor").Boolean).IsFalse();
            await Assert
                .That(table.Get("decltype").String)
                .IsEqualTo(typeof(MethodMemberDescriptorHost).FullName);
            await Assert.That(table.Get("static").Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringArrayConstructorIncludesArrayMetadata()
        {
            EnsureArrayUserDataRegistered();
            ConstructorInfo ctor =
                MethodMemberDescriptorArrayMetadata.Int32TwoDimensionalConstructor;
            MethodMemberDescriptor descriptor = new(ctor, InteropAccessMode.Reflection);

            Script script = new();
            Table table = new(script);

            descriptor.PrepareForWiring(table);

            await Assert.That(table.Get("ctor").Boolean).IsTrue();
            await Assert.That(table.Get("arraytype").String).IsEqualTo(typeof(int).FullName);
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringThrowsWhenTableNull()
        {
            MethodInfo method = MethodMemberDescriptorHostMetadata.SetName;
            MethodMemberDescriptor descriptor = new(method, InteropAccessMode.Reflection);

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                descriptor.PrepareForWiring(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }

    internal sealed class MethodMemberDescriptorHost
    {
        public string LastName { get; private set; } = string.Empty;
        public int LastProduct { get; private set; }
        public int LastInputValue { get; private set; }
        public bool HiddenHelperCalled { get; private set; }

        public MethodMemberDescriptorHost() { }

        public MethodMemberDescriptorHost(string name)
        {
            LastName = name;
        }

        public void SetName(string name)
        {
            LastName = name;
        }

        public int Multiply(int left, int right)
        {
            LastProduct = left * right;
            return LastProduct;
        }

        public bool TryDouble(int value, out int doubled)
        {
            LastInputValue = value;
            doubled = value * 2;
            return true;
        }

        private void HiddenHelper()
        {
            HiddenHelperCalled = true;
        }

        public static int Sum(int left, int right)
        {
            return left + right;
        }

        internal static MethodInfo GetHiddenHelperMethod()
        {
            Expression<Action<MethodMemberDescriptorHost>> call = host => host.HiddenHelper();
            return ((MethodCallExpression)call.Body).Method;
        }
    }

    internal static class MethodMemberDescriptorHostMetadata
    {
        internal static MethodInfo SetName { get; } =
            typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.SetName)
            );

        internal static MethodInfo Multiply { get; } =
            typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.Multiply)
            );

        internal static MethodInfo TryDouble { get; } =
            typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.TryDouble)
            );

        internal static MethodInfo Sum { get; } =
            typeof(MethodMemberDescriptorHost).GetMethod(
                nameof(MethodMemberDescriptorHost.Sum),
                new[] { typeof(int), typeof(int) }
            );

        internal static MethodInfo HiddenHelper { get; } =
            MethodMemberDescriptorHost.GetHiddenHelperMethod();
    }

    internal static class MethodMemberDescriptorArrayMetadata
    {
        internal static ConstructorInfo Int32TwoDimensionalConstructor { get; } =
            typeof(int[,]).GetConstructor(new[] { typeof(int), typeof(int) });
    }

    internal static class MethodMemberDescriptorTestExtensionsMetadata
    {
        internal static MethodInfo Decorate { get; } =
            typeof(MethodMemberDescriptorTestExtensions).GetMethod(
                nameof(MethodMemberDescriptorTestExtensions.Decorate)
            );
    }

    internal static class GenericMethodHostMetadata
    {
        internal static MethodInfo GenericIdentity { get; } =
            typeof(GenericMethodHost).GetMethod(nameof(GenericMethodHost.Identity));
    }

    internal static class MethodMemberDescriptorTestExtensions
    {
        public static string Decorate(this MethodMemberDescriptorHost host, string suffix)
        {
            return host.LastName + suffix;
        }
    }

    internal static class GenericMethodHost
    {
        public static T Identity<T>(T value)
        {
            return value;
        }
    }
}
