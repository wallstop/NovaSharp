namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Errors;
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
        }

        [Test]
        public void ExecuteCachesResolvedOverloadForMatchingUserDataArguments()
        {
            MethodInfo userDataOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeHost)
            );
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
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

            var callback = descriptor.GetCallback(script, host);
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
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            MethodInfo extensionMethod = typeof(OverloadedMethodHostExtensions).GetMethod(
                nameof(OverloadedMethodHostExtensions.AppendSuffix)
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
            var callback = descriptor.GetCallback(script, host);

            DynValue result = callback(context, TestHelpers.CreateArguments(DynValue.NewString("!")));

            Assert.That(result.String, Is.EqualTo("ext!"));
        }

        [Test]
        public void ExecuteThrowsWhenExtensionMethodsAreIgnored()
        {
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            MethodInfo extensionMethod = typeof(OverloadedMethodHostExtensions).GetMethod(
                nameof(OverloadedMethodHostExtensions.AppendSuffix)
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
            var callback = descriptor.GetCallback(script, host);

            Assert.That(
                () => callback(context, TestHelpers.CreateArguments(DynValue.NewString("!"))),
                Throws.TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("function call doesn't match any overload")
            );
        }

        [Test]
        public void ExecutePrefersVarArgsOverloadWhenMultipleArgumentsRemain()
        {
            MethodInfo varArgOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.JoinMany)
            );
            MethodInfo singleOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.JoinSingle)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(
                varArgOverload,
                singleOverload
            );

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            var callback = descriptor.GetCallback(script, host);

            DynValue result = callback(
                context,
                TestHelpers.CreateArguments(
                    DynValue.NewString("-"),
                    DynValue.NewString("a"),
                    DynValue.NewString("b"),
                    DynValue.NewString("c")
                )
            );

            Assert.That(result.String, Is.EqualTo("a-b-c"));
        }

        [Test]
        public void GetCallbackFunctionReturnsNamedDelegate()
        {
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
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
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();

            DynValue value = descriptor.GetValue(script, host);

            Assert.Multiple(() =>
            {
                Assert.That(value.Type, Is.EqualTo(DataType.Function));
                DynValue result = value.Function.Call(3);
                Assert.That(result.String, Is.EqualTo("num:3"));
            });
        }

        [Test]
        public void SetValueThrowsWhenWriteIsAttempted()
        {
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
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
            MethodInfo varArgOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.JoinMany)
            );
            MethodInfo singleOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.JoinSingle)
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
            MethodInfo singleOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.JoinSingle)
            );
            OverloadedMethodMemberDescriptor descriptor = new(
                singleOverload.Name,
                singleOverload.DeclaringType
            );
            descriptor.AddOverload(
                new MethodMemberDescriptor(singleOverload, InteropAccessMode.LazyOptimized)
            );

            ((IOptimizableDescriptor)descriptor).Optimize();
        }

        [Test]
        public void ExecuteThrowsWhenNoOverloadMatchesArguments()
        {
            MethodInfo numberOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.DescribeNumber)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(numberOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            var callback = descriptor.GetCallback(script, host);

            Assert.That(
                () => callback(context, TestHelpers.CreateArguments()),
                Throws.TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("function call doesn't match any overload")
            );
        }

        [Test]
        public void SingleOverloadFastPathExecutesWithoutSorting()
        {
            MethodInfo singleOverload = typeof(OverloadedMethodHost).GetMethod(
                nameof(OverloadedMethodHost.JoinSingle)
            );
            OverloadedMethodMemberDescriptor descriptor = CreateDescriptor(singleOverload);

            Script script = new Script();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            OverloadedMethodHost host = new();
            var callback = descriptor.GetCallback(script, host);

            DynValue result = callback(context, TestHelpers.CreateArguments(DynValue.NewString("value")));

            Assert.That(result.String, Is.EqualTo("value"));
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
}
