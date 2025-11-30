#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Hardwire.Utils;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

    public sealed class HardwireParameterDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorMapsFlagsAndTypes()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(hasDefault: true);

            await Assert.That(descriptor.ParamType).IsEqualTo(typeof(string).FullName);
            await Assert.That(descriptor.HasDefaultValue).IsTrue();
            await Assert.That(descriptor.IsOut).IsFalse();
            await Assert.That(descriptor.IsRef).IsFalse();
            await Assert.That(descriptor.Expression).IsTypeOf<CodeObjectCreateExpression>();
        }

        [global::TUnit.Core.Test]
        public async Task SetTempVarStoresNameForOutParameter()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(isOut: true);

            descriptor.SetTempVar("tmp");

            await Assert.That(descriptor.TempVarName).IsEqualTo("tmp");
        }

        [global::TUnit.Core.Test]
        public async Task SetTempVarThrowsForByValueParameter()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                descriptor.SetTempVar("tmp")
            );
            await Assert
                .That(exception.Message.Contains("byval", StringComparison.Ordinal))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task LoadDescriptorsFromTableCreatesSequentialList()
        {
            Table list = new(owner: null);
            list.Append(DynValue.NewTable(CreateParameterTable("first", isRef: true)));
            list.Append(DynValue.NewTable(CreateParameterTable("second", isOut: true)));

            IReadOnlyList<HardwireParameterDescriptor> descriptors =
                HardwireParameterDescriptor.LoadDescriptorsFromTable(list);

            await Assert.That(descriptors.Count).IsEqualTo(2);
            await Assert.That(descriptors[0].IsRef).IsTrue();
            await Assert.That(descriptors[1].IsOut).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task LoadDescriptorsFromTableThrowsWhenTableNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                HardwireParameterDescriptor.LoadDescriptorsFromTable(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        public async Task LoadDescriptorsFromTableThrowsWhenEntryNotTable()
        {
            Table list = new(owner: null);
            list.Append(DynValue.NewNumber(1));

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                HardwireParameterDescriptor.LoadDescriptorsFromTable(list)
            );

            await Assert.That(exception.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenTableNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new HardwireParameterDescriptor(null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("tpar");
        }

        [global::TUnit.Core.Test]
        public async Task DefaultValueParameterCreatesDefaultValuePlaceholder()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(hasDefault: true);

            await Assert.That(descriptor.HasDefaultValue).IsTrue();
            await Assert.That(descriptor.Expression).IsTypeOf<CodeObjectCreateExpression>();

            CodeObjectCreateExpression parameterFactory = (CodeObjectCreateExpression)
                descriptor.Expression;
            await Assert
                .That(parameterFactory.CreateType.BaseType)
                .IsEqualTo(typeof(ParameterDescriptor).FullName);

            CodeExpression defaultArgument = parameterFactory.Parameters[3];
            await Assert.That(defaultArgument).IsTypeOf<CodeObjectCreateExpression>();

            CodeObjectCreateExpression defaultValueFactory =
                (CodeObjectCreateExpression)defaultArgument;
            await Assert
                .That(defaultValueFactory.CreateType.BaseType)
                .IsEqualTo(typeof(DefaultValue).FullName);
        }

        private static HardwireParameterDescriptor CreateDescriptor(
            bool hasDefault = false,
            bool isOut = false,
            bool isRef = false
        )
        {
            Table parameterTable = CreateParameterTable("value", hasDefault, isOut, isRef);

            return new HardwireParameterDescriptor(parameterTable);
        }

        private static Table CreateParameterTable(
            string name,
            bool hasDefault = false,
            bool isOut = false,
            bool isRef = false
        )
        {
            Table table = new(owner: null);
            table.Set("name", DynValue.NewString(name));
            table.Set("origtype", DynValue.NewString(typeof(string).FullName));
            table.Set("default", DynValue.NewBoolean(hasDefault));
            table.Set("out", DynValue.NewBoolean(isOut));
            table.Set("ref", DynValue.NewBoolean(isRef));
            table.Set("varargs", DynValue.NewBoolean(false));
            table.Set("type", DynValue.NewString(typeof(string).FullName));
            table.Set("restricted", DynValue.NewBoolean(false));

            return table;
        }
    }
}
#pragma warning restore CA2007
