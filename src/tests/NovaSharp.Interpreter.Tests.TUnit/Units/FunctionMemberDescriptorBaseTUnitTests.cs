namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class FunctionMemberDescriptorBaseTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CreateCallbackDynValueFromMethodInfo()
        {
            Script script = new();
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );

            DynValue callback = FunctionMemberDescriptorBase.CreateCallbackDynValue(script, method);

            await Assert.That(callback.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
            DynValue result = script.Call(callback, 10, 32);
            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CreateCallbackDynValueFromInstanceMethod()
        {
            Script script = new();
            SampleClass instance = new() { Multiplier = 3 };
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.MultiplyBy),
                BindingFlags.Public | BindingFlags.Instance
            );

            DynValue callback = FunctionMemberDescriptorBase.CreateCallbackDynValue(
                script,
                method,
                instance
            );

            await Assert.That(callback.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
            DynValue result = script.Call(callback, 14);
            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCallbackAsDynValueReturnsClrFunction()
        {
            Script script = new();
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            DynValue callback = descriptor.GetCallbackAsDynValue(script);

            await Assert.That(callback.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsWithUserDataArrayPassthrough()
        {
            UserData.RegisterType<SampleClass>();
            UserData.RegisterType<int[]>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local arr = {1, 2, 3, 4, 5}
                return obj.SumVarArgs(1, 2, 3)
            "
            );

            await Assert.That(result.Number).IsEqualTo(6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsWithUserDataArrayPassthroughSingleArg()
        {
            UserData.RegisterType<SampleClass>();
            UserData.RegisterType<int[]>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();
            int[] testArray = { 10, 20, 12 };
            script.Globals["arr"] = UserData.Create(testArray);

            DynValue result = script.DoString(
                @"
                return obj.SumVarArgs(arr)
            "
            );

            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithRefAndOutParameters()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local upper, concat, lower = obj.ManipulateString('Hello', 'World')
                return upper .. '|' .. concat .. '|' .. lower
            "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("HELLO|HelloWorld|hello")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodReturningVoidWithOutParams()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local nil_val, out1, out2 = obj.VoidWithOut(5, 10)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
            "
            );

            await Assert.That(result.String).IsEqualTo("nil|5|10").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetValueFromDescriptorReturnsCallback()
        {
            Script script = new();
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            DynValue value = descriptor.GetValue(script, null);

            await Assert.That(value.Type).IsEqualTo(DataType.ClrFunction).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetValueOnReadOnlyThrowsScriptRuntimeException()
        {
            Script script = new();
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            await Assert
                .That(() => descriptor.SetValue(script, null, DynValue.NewNumber(42)))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MemberAccessHasReadAndExecute()
        {
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            MemberDescriptorAccess access = descriptor.MemberAccess;

            await Assert
                .That(access.HasFlag(MemberDescriptorAccess.CanRead))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(access.HasFlag(MemberDescriptorAccess.CanExecute))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(access.HasFlag(MemberDescriptorAccess.CanWrite))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCallbackFunctionReturnsNamedCallback()
        {
            Script script = new();
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            CallbackFunction callback = descriptor.GetCallbackFunction(script);

            await Assert.That(callback.Name).IsEqualTo("AddNumbers").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCallbackReturnsWorkingDelegate()
        {
            Script script = new();
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            Func<Execution.ScriptExecutionContext, CallbackArguments, DynValue> callback =
                descriptor.GetCallback(script);

            await Assert.That(callback).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParametersPropertyReturnsCorrectCount()
        {
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            await Assert.That(descriptor.Parameters.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(descriptor.Parameters[0].Type)
                .IsEqualTo(typeof(int))
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.Parameters[1].Type)
                .IsEqualTo(typeof(int))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task VarArgsElementTypeIsSetCorrectly()
        {
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.SumVarArgs),
                BindingFlags.Public | BindingFlags.Instance
            );
            MethodMemberDescriptor descriptor = new(method);

            await Assert
                .That(descriptor.VarArgsArrayType)
                .IsEqualTo(typeof(int[]))
                .ConfigureAwait(false);
            await Assert
                .That(descriptor.VarArgsElementType)
                .IsEqualTo(typeof(int))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SortDiscriminantIsBuiltFromParameterTypes()
        {
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            string expected = $"{typeof(int).FullName}:{typeof(int).FullName}";
            await Assert
                .That(descriptor.SortDiscriminant)
                .IsEqualTo(expected)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SortDiscriminantIsEmptyForParameterlessMethod()
        {
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.GetFortyTwo),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            await Assert
                .That(descriptor.SortDiscriminant)
                .IsEqualTo(string.Empty)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExtensionMethodTypeShouldBeNullForNonExtensionMethods()
        {
            MethodInfo method = typeof(SampleClass).GetMethod(
                nameof(SampleClass.AddNumbers),
                BindingFlags.Public | BindingFlags.Static
            );
            MethodMemberDescriptor descriptor = new(method);

            await Assert.That(descriptor.ExtensionMethodType).IsNull().ConfigureAwait(false);
        }

        internal sealed class SampleClass
        {
            public int Multiplier { get; set; } = 1;
            private int _callCount;

            public static int AddNumbers(int a, int b)
            {
                return a + b;
            }

            public static int GetFortyTwo()
            {
                return 42;
            }

            public int MultiplyBy(int value)
            {
                return value * Multiplier;
            }

            public int SumVarArgs(params int[] numbers)
            {
                _callCount++;
                int sum = 0;
                foreach (int n in numbers)
                {
                    sum += n;
                }
                return sum;
            }

            public string ManipulateString(
                string input,
                ref string tobeconcat,
                out string lowercase
            )
            {
                _callCount++;
                tobeconcat = input + tobeconcat;
                lowercase = InvariantString.ToLowerInvariantIfNeeded(input);
                return input.ToUpperInvariant();
            }

            public void VoidWithOut(int a, int b, out int sum, out int product)
            {
                _callCount++;
                sum = a;
                product = b;
            }
        }
    }
}
