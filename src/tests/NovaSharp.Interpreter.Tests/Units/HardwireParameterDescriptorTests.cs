namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using NovaSharp.Hardwire.Utils;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireParameterDescriptorTests
    {
        [Test]
        public void ConstructorMapsFlagsAndTypes()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(hasDefault: true);

            Assert.That(descriptor.ParamType, Is.EqualTo(typeof(string).FullName));
            Assert.That(descriptor.HasDefaultValue, Is.True);
            Assert.That(descriptor.IsOut, Is.False);
            Assert.That(descriptor.IsRef, Is.False);
            Assert.That(descriptor.Expression, Is.TypeOf<CodeObjectCreateExpression>());
        }

        [Test]
        public void SetTempVarCapturesVariableForOutParameter()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(isOut: true);

            descriptor.SetTempVar("tmp");

            Assert.That(descriptor.TempVarName, Is.EqualTo("tmp"));
        }

        [Test]
        public void SetTempVarThrowsForByValueParameter()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor();

            Assert.That(
                () => descriptor.SetTempVar("tmp"),
                Throws.InvalidOperationException.With.Message.Contains("byval")
            );
        }

        [Test]
        public void LoadDescriptorsFromTableCreatesSequentialList()
        {
            Table list = new(owner: null);
            list.Append(DynValue.NewTable(CreateParameterTable("first", isRef: true)));
            list.Append(DynValue.NewTable(CreateParameterTable("second", isOut: true)));

            IReadOnlyList<HardwireParameterDescriptor> descriptors =
                HardwireParameterDescriptor.LoadDescriptorsFromTable(list);

            Assert.That(descriptors, Has.Count.EqualTo(2));
            Assert.That(descriptors[0].IsRef, Is.True);
            Assert.That(descriptors[1].IsOut, Is.True);
        }

        [Test]
        public void LoadDescriptorsFromTableThrowsWhenTableNull()
        {
            Assert.That(
                () => HardwireParameterDescriptor.LoadDescriptorsFromTable(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("t")
            );
        }

        [Test]
        public void LoadDescriptorsFromTableThrowsWhenEntryNotTable()
        {
            Table list = new(owner: null);
            list.Append(DynValue.NewNumber(1));

            Assert.That(
                () => HardwireParameterDescriptor.LoadDescriptorsFromTable(list),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("t")
            );
        }

        [Test]
        public void ConstructorThrowsWhenTableNull()
        {
            Assert.That(
                () => new HardwireParameterDescriptor(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tpar")
            );
        }

        [Test]
        public void DefaultValueParameterCreatesDefaultValuePlaceholder()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(hasDefault: true);

            Assert.That(descriptor.HasDefaultValue, Is.True);

            CodeObjectCreateExpression expression = AssertCast<CodeObjectCreateExpression>(
                descriptor.Expression
            );
            Assert.That(
                expression.CreateType.BaseType,
                Is.EqualTo(typeof(ParameterDescriptor).FullName)
            );

            CodeExpression defaultArgument = expression.Parameters[3];
            CodeObjectCreateExpression defaultValueExpression =
                AssertCast<CodeObjectCreateExpression>(defaultArgument);

            Assert.That(
                defaultValueExpression.CreateType.BaseType,
                Is.EqualTo(typeof(DefaultValue).FullName)
            );
        }

        private static HardwireParameterDescriptor CreateDescriptor(
            bool hasDefault = false,
            bool isOut = false,
            bool isRef = false
        )
        {
            Table parameterTable = CreateParameterTable(
                "value",
                hasDefault: hasDefault,
                isOut: isOut,
                isRef: isRef
            );

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

        private static T AssertCast<T>(CodeExpression expression)
            where T : CodeExpression
        {
            Assert.That(expression, Is.TypeOf<T>());
            return (T)expression;
        }
    }
}
