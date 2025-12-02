namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    public sealed class ConfigPropertyAssignerTUnitTests
    {
        [SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Instances are created when PropertyTableAssigner hydrates sub-objects."
        )]
        private sealed class MySubclass
        {
            [NovaSharpProperty]
            public string MyString { get; set; }

            [NovaSharpProperty("number")]
            public int MyNumber { get; private set; }
        }

        [SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "PropertyTableAssigner creates this type during tests."
        )]
        private sealed class MyClass
        {
            [NovaSharpProperty]
            public string MyString { get; set; }

            [NovaSharpProperty("number")]
            public int MyNumber { get; private set; }

            [NovaSharpProperty]
            internal Table SomeTable { get; private set; }

            [NovaSharpProperty]
            public DynValue NativeValue { get; private set; }

            [NovaSharpProperty]
            public MySubclass SubObj { get; private set; }
        }

        [global::TUnit.Core.Test]
        public async Task ConfigPropSimpleAssign()
        {
            MyClass assigned = AssignFromLua(
                @"
                {
                    class = 'oohoh',
                    myString = 'ciao',
                    number = 3,
                    some_table = {},
                    nativeValue = function() end,
                    subObj = { number = 15, myString = 'hi' },
                }"
            );

            await Assert.That(assigned.MyNumber).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(assigned.MyString).IsEqualTo("ciao").ConfigureAwait(false);
            await Assert
                .That(assigned.NativeValue.Type)
                .IsEqualTo(DataType.Function)
                .ConfigureAwait(false);
            await Assert.That(assigned.SubObj.MyNumber).IsEqualTo(15).ConfigureAwait(false);
            await Assert.That(assigned.SubObj.MyString).IsEqualTo("hi").ConfigureAwait(false);
            await Assert.That(assigned.SomeTable).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConfigPropThrowsOnInvalid()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                _ = AssignFromLua(
                    @"
                    {
                        class = 'oohoh',
                        myString = 'ciao',
                        number = 3,
                        some_table = {},
                        invalid = 3,
                        nativeValue = function() end,
                    }"
                );
            });

            await Assert.That(exception.Message).Contains("invalid").ConfigureAwait(false);
        }

        private static MyClass AssignFromLua(string tableDefinition)
        {
            Script script = new(default(CoreModules));
            DynValue tableValue = script.DoString("return " + tableDefinition);
            if (tableValue.Type != DataType.Table)
            {
                throw new InvalidOperationException("Lua expression did not return a table.");
            }

            PropertyTableAssigner<MyClass> root = new("class");
            PropertyTableAssigner<MySubclass> child = new();
            root.SetSubassigner(child);

            MyClass instance = new();
            root.AssignObject(instance, tableValue.Table);
            return instance;
        }
    }
}
