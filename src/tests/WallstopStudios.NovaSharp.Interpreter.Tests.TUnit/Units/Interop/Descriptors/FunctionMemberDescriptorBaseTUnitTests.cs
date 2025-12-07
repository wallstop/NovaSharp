namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Interop.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

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

            Func<ScriptExecutionContext, CallbackArguments, DynValue> callback =
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

        // ========================================
        // Out/Ref Parameter Edge Case Tests
        // ========================================

        [global::TUnit.Core.Test]
        public async Task MethodWithOutParamsCalledMultipleTimes()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            // Call multiple times to test array pool reuse
            for (int i = 1; i <= 10; i++)
            {
                int expectedSum = i;
                int expectedProduct = i * 2;

                DynValue result = script.DoString(
                    $@"
                    local nil_val, out1, out2 = obj.VoidWithOut({i}, {i * 2})
                    return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
                "
                );

                await Assert
                    .That(result.String)
                    .IsEqualTo($"nil|{expectedSum}|{expectedProduct}")
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithRefParamsCalledMultipleTimes()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            // Call multiple times to test array pool reuse
            for (int i = 1; i <= 10; i++)
            {
                string input = $"Test{i}";
                string refValue = $"Suffix{i}";
                string expectedUpper = input.ToUpperInvariant();
                string expectedConcat = input + refValue;
                string expectedLower = InvariantString.ToLowerInvariantIfNeeded(input);

                DynValue result = script.DoString(
                    $@"
                    local upper, concat, lower = obj.ManipulateString('{input}', '{refValue}')
                    return upper .. '|' .. concat .. '|' .. lower
                "
                );

                await Assert
                    .That(result.String)
                    .IsEqualTo($"{expectedUpper}|{expectedConcat}|{expectedLower}")
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithOutParamsZeroValues()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local nil_val, out1, out2 = obj.VoidWithOut(0, 0)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
            "
            );

            await Assert.That(result.String).IsEqualTo("nil|0|0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithOutParamsNegativeValues()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local nil_val, out1, out2 = obj.VoidWithOut(-100, -200)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
            "
            );

            await Assert.That(result.String).IsEqualTo("nil|-100|-200").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithOutParamsLargeValues()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local nil_val, out1, out2 = obj.VoidWithOut(2147483647, -2147483648)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
            "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("nil|2147483647|-2147483648")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithOnlyOutParamsNoInput()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local nil_val, out1, out2, out3 = obj.GetMultipleOutValues()
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2 .. '|' .. tostring(out3)
            "
            );

            await Assert.That(result.String).IsEqualTo("nil|42|hello|true").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithMixedRefOutAndReturnValue()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local ret, ref_out, pure_out = obj.ComplexRefOutMethod(10, 5)
                return ret .. '|' .. ref_out .. '|' .. pure_out
            "
            );

            // Method returns a + b = 15, ref is multiplied by 2 = 10, out is a * b = 50
            await Assert.That(result.String).IsEqualTo("15|10|50").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithSingleOutParam()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local ret, parsed = obj.TryParseInt('42')
                return tostring(ret) .. '|' .. parsed
            "
            );

            await Assert.That(result.String).IsEqualTo("true|42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithSingleOutParamFailedParse()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            DynValue result = script.DoString(
                @"
                local ret, parsed = obj.TryParseInt('not_a_number')
                return tostring(ret) .. '|' .. parsed
            "
            );

            await Assert.That(result.String).IsEqualTo("false|0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MethodWithOutParamsInterleavedWithDifferentMethods()
        {
            UserData.RegisterType<SampleClass>();

            Script script = new();
            script.Globals["obj"] = new SampleClass();

            // Interleave calls to different methods to stress test pool
            DynValue result = script.DoString(
                @"
                local results = {}
                
                -- First: void with out
                local nil1, a, b = obj.VoidWithOut(1, 2)
                table.insert(results, tostring(nil1) .. '|' .. a .. '|' .. b)
                
                -- Second: regular method
                local sum = obj.AddNumbers(3, 4)
                table.insert(results, tostring(sum))
                
                -- Third: void with out again
                local nil2, c, d = obj.VoidWithOut(5, 6)
                table.insert(results, tostring(nil2) .. '|' .. c .. '|' .. d)
                
                -- Fourth: ref/out combo
                local upper, concat, lower = obj.ManipulateString('X', 'Y')
                table.insert(results, upper .. '|' .. concat .. '|' .. lower)
                
                -- Fifth: void with out again
                local nil3, e, f = obj.VoidWithOut(7, 8)
                table.insert(results, tostring(nil3) .. '|' .. e .. '|' .. f)
                
                return table.concat(results, ';')
            "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("nil|1|2;7;nil|5|6;X|XY|x;nil|7|8")
                .ConfigureAwait(false);
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

            public void GetMultipleOutValues(out int number, out string text, out bool flag)
            {
                _callCount++;
                number = 42;
                text = "hello";
                flag = true;
            }

            public int ComplexRefOutMethod(int a, ref int b, out int c)
            {
                _callCount++;
                int result = a + b;
                b = b * 2;
                c = a * b / 2; // After doubling b, so c = a * (b * 2) / 2 = a * b
                return result;
            }

            public bool TryParseInt(string input, out int result)
            {
                _callCount++;
                return int.TryParse(input, out result);
            }
        }
    }
}
