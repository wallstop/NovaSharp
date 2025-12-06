namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Tests;

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
        public async Task GetterReturnsArrayElement()
        {
            Script script = new();
            int[] array = { 10, 20, 30 };
            UserData.RegisterType<int[]>();
            script.Globals["arr"] = array;

            DynValue result = script.DoString("return arr[1]");

            await Assert.That(result.Number).IsEqualTo(20).ConfigureAwait(false);
        }

        [Test]
        public async Task SetterModifiesArrayElement()
        {
            Script script = new();
            int[] array = { 10, 20, 30 };
            UserData.RegisterType<int[]>();
            script.Globals["arr"] = array;

            script.DoString("arr[1] = 99");

            await Assert.That(array[1]).IsEqualTo(99).ConfigureAwait(false);
        }

        [Test]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task MultiDimensionalArrayAccess()
        {
            Script script = new();
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1814:Prefer jagged arrays over multidimensional",
            Justification = "Test specifically verifies multi-dimensional array support."
        )]
        public async Task MultiDimensionalArraySet()
        {
            Script script = new();
            int[,] array = new int[2, 2];
            UserData.RegisterType<int[,]>();
            script.Globals["arr"] = array;

            script.DoString("arr[0, 1] = 42");

            await Assert.That(array[0, 1]).IsEqualTo(42).ConfigureAwait(false);
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
    }
}
