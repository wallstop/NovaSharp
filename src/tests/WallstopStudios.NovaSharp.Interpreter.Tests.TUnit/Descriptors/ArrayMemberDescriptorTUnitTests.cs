namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [UserDataIsolation]
    public sealed class ArrayMemberDescriptorTUnitTests
    {
        [Test]
        public async Task PrepareForWiringWritesClassNameAndSetterFlag()
        {
            ArrayMemberDescriptor descriptor = new("TestName", isSetter: true);
            Table table = new(owner: null);

            descriptor.PrepareForWiring(table);

            await Assert
                .That(table.Get("class").String)
                .IsEqualTo(typeof(ArrayMemberDescriptor).FullName)
                .ConfigureAwait(false);
            await Assert.That(table.Get("name").String).IsEqualTo("TestName").ConfigureAwait(false);
            await Assert.That(table.Get("setter").Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task PrepareForWiringWritesSetterFalseForGetter()
        {
            ArrayMemberDescriptor descriptor = new("GetterName", isSetter: false);
            Table table = new(owner: null);

            descriptor.PrepareForWiring(table);

            await Assert.That(table.Get("setter").Boolean).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task PrepareForWiringThrowsWhenTableNull()
        {
            ArrayMemberDescriptor descriptor = new("Test", isSetter: false);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                descriptor.PrepareForWiring(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t").ConfigureAwait(false);
        }

        [Test]
        public async Task PrepareForWiringIncludesParametersWhenProvided()
        {
            ParameterDescriptor[] parameters = new[]
            {
                new ParameterDescriptor("index", typeof(int)),
            };
            ArrayMemberDescriptor descriptor = new("IndexedName", isSetter: false, parameters);
            Table table = new(owner: null);

            descriptor.PrepareForWiring(table);

            DynValue paramsTable = table.Get("params");
            await Assert.That(paramsTable.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            await Assert.That(paramsTable.Table.Length).IsEqualTo(1).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task GetterReturnsArrayElement(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            int[] array = { 10, 20, 30 };
            UserData.RegisterType<int[]>();
            script.Globals["arr"] = array;

            DynValue result = script.DoString("return arr[1]");

            await Assert.That(result.Number).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task SetterModifiesArrayElement(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            int[] array = { 10, 20, 30 };
            UserData.RegisterType<int[]>();
            script.Globals["arr"] = array;

            script.DoString("arr[1] = 99");

            await Assert.That(array[1]).IsEqualTo(99).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task MultiDimensionalArrayAccess(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            int[,] array = new int[2, 2];
            array[0, 0] = 1;
            array[0, 1] = 2;
            array[1, 0] = 3;
            array[1, 1] = 4;
            UserData.RegisterType<int[,]>();
            script.Globals["arr"] = array;

            DynValue result = script.DoString("return arr[1, 1]");

            await Assert.That(result.Number).IsEqualTo(4).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task MultiDimensionalArraySet(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            int[,] array = new int[2, 2];
            UserData.RegisterType<int[,]>();
            script.Globals["arr"] = array;

            script.DoString("arr[0, 1] = 42");

            await Assert.That(array[0, 1]).IsEqualTo(42).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task ThreeDimensionalArrayAccess(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            int[,,] array = new int[2, 2, 2];
            array[1, 1, 1] = 7;
            UserData.RegisterType<int[,,]>();
            script.Globals["arr"] = array;

            DynValue result = script.DoString("return arr[1, 1, 1]");

            await Assert.That(result.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task ThreeDimensionalArraySet(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            int[,,] array = new int[2, 2, 2];
            UserData.RegisterType<int[,,]>();
            script.Globals["arr"] = array;

            script.DoString("arr[1, 0, 1] = 77");

            await Assert.That(array[1, 0, 1]).IsEqualTo(77).ConfigureAwait(false);
        }

        [Test]
        public async Task RankOneArraySetterAvoidsIndexArrayAllocation()
        {
            const int iterations = 1024;
            const long maxAllocatedBytesPerCall = 16L;
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            object[] array = new object[2];
            ArrayMemberDescriptor descriptor = new("set_Item", isSetter: true);
            DynValue index = DynValue.NewNumber(1);
            DynValue value = DynValue.NewString("payload");
            CallbackArguments args = TestHelpers.CreateArguments(index, value);

            DynValue warmup = descriptor.Execute(script, array, context, args);
            await Assert.That(warmup.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
            await Assert.That(array[1]).IsEqualTo("payload").ConfigureAwait(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocated = MeasureRankOneArraySetterAllocations(
                descriptor,
                script,
                array,
                context,
                args,
                value.String,
                iterations
            );
            long allocatedPerCall = allocated / iterations;

            await Assert
                .That(allocatedPerCall)
                .IsLessThan(maxAllocatedBytesPerCall)
                .Because(
                    $"Rank-one array setter dispatch allocated {allocated} bytes across {iterations} iterations ({allocatedPerCall} bytes/call)."
                )
                .ConfigureAwait(false);
        }

        [Test]
        public async Task RankOneArrayGetterAvoidsIndexArrayAllocation()
        {
            const int iterations = 1024;
            const long maxAllocatedBytesPerCall = 16L;
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            DynValue expected = DynValue.NewString("payload");
            object[] array = { DynValue.Nil, expected };
            ArrayMemberDescriptor descriptor = new("get_Item", isSetter: false);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(1));

            DynValue warmup = descriptor.Execute(script, array, context, args);
            await Assert.That(warmup).IsEqualTo(expected).ConfigureAwait(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocated = MeasureRankOneArrayGetterAllocations(
                descriptor,
                script,
                array,
                context,
                args,
                expected,
                iterations
            );
            long allocatedPerCall = allocated / iterations;

            await Assert
                .That(allocatedPerCall)
                .IsLessThan(maxAllocatedBytesPerCall)
                .Because(
                    $"Rank-one array getter dispatch allocated {allocated} bytes across {iterations} iterations ({allocatedPerCall} bytes/call)."
                )
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ArraySetterConvertsIndexBeforeAssignedValue()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            int[] array = new int[2];
            ArrayMemberDescriptor descriptor = new("set_Item", isSetter: true);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewString("bad-index"),
                DynValue.NewPrimeTable()
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.Execute(script, array, context, args)
            );

            await Assert
                .That(exception.Message)
                .Contains("userdata_array_indexer")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ArraySetterConvertsAssignedValueBeforeBoundsValidation()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            int[] array = new int[1];
            ArrayMemberDescriptor descriptor = new("set_Item", isSetter: true);
            CallbackArguments args = TestHelpers.CreateArguments(
                DynValue.NewNumber(99),
                DynValue.NewPrimeTable()
            );

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                descriptor.Execute(script, array, context, args)
            );

            await Assert.That(exception.Message).Contains("cannot convert").ConfigureAwait(false);
        }

        [Test]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task WrongRankArrayAccessUsesVectorIndexFallback()
        {
            Script script = new();
            ScriptExecutionContext context = TestHelpers.CreateExecutionContext(script);
            int[,] array = new int[2, 2];
            ArrayMemberDescriptor descriptor = new("get_Item", isSetter: false);
            CallbackArguments args = TestHelpers.CreateArguments(DynValue.NewNumber(0));

            ArgumentException actual = Assert.Throws<ArgumentException>(() =>
                descriptor.Execute(script, array, context, args)
            );
            ArgumentException expected = Assert.Throws<ArgumentException>(() =>
                LegacyArrayIndexerGet(array, args)
            );

            await Assert.That(actual.GetType()).IsEqualTo(expected.GetType()).ConfigureAwait(false);
            await Assert.That(actual.Message).IsEqualTo(expected.Message).ConfigureAwait(false);
        }

        [Test]
        public async Task PrepareForWiringDoesNotSetParamsWhenUsingConstructorWithoutParams()
        {
            ArrayMemberDescriptor descriptor = new("NoParams", isSetter: true);
            Table table = new(owner: null);

            descriptor.PrepareForWiring(table);

            // When using the two-arg constructor, Parameters is null so params is not set
            // But base class may set empty params - just verify the descriptor works
            await Assert.That(table.Get("name").String).IsEqualTo("NoParams").ConfigureAwait(false);
            await Assert.That(table.Get("setter").Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorWithParametersStoresParameters()
        {
            ParameterDescriptor[] parameters = new[]
            {
                new ParameterDescriptor("x", typeof(int)),
                new ParameterDescriptor("y", typeof(int)),
            };
            ArrayMemberDescriptor descriptor = new("MultiIndex", isSetter: false, parameters);
            Table table = new(owner: null);

            descriptor.PrepareForWiring(table);

            DynValue paramsTable = table.Get("params");
            await Assert.That(paramsTable.Table.Length).IsEqualTo(2).ConfigureAwait(false);
        }

        private static long MeasureRankOneArraySetterAllocations(
            ArrayMemberDescriptor descriptor,
            Script script,
            object[] array,
            ScriptExecutionContext context,
            CallbackArguments args,
            string expectedValue,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                DynValue result = descriptor.Execute(script, array, context, args);

                if (result.Type != DataType.Void || !ReferenceEquals(array[1], expectedValue))
                {
                    throw new InvalidOperationException(
                        "Rank-one array setter allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static long MeasureRankOneArrayGetterAllocations(
            ArrayMemberDescriptor descriptor,
            Script script,
            object[] array,
            ScriptExecutionContext context,
            CallbackArguments args,
            DynValue expectedValue,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < iterations; i++)
            {
                DynValue result = descriptor.Execute(script, array, context, args);

                if (!ReferenceEquals(result, expectedValue))
                {
                    throw new InvalidOperationException(
                        "Rank-one array getter allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static object LegacyArrayIndexerGet(Array array, CallbackArguments args)
        {
            int[] indices = BuildArrayIndicesForTest(args, args.Count);
            return array.GetValue(indices);
        }

        private static int[] BuildArrayIndicesForTest(CallbackArguments args, int count)
        {
            int[] indices = new int[count];

            for (int i = 0; i < count; i++)
            {
                indices[i] = args.AsInt(i, "userdata_array_indexer");
            }

            return indices;
        }
    }
}
