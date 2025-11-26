namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ModuleRegisterTests
    {
        [Test]
        public void RegisterCoreModulesRemovesWarnWhenProfileDoesNotSupportIt()
        {
            ScriptOptions options = new ScriptOptions
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua53,
            };
            Script script = new Script(CoreModules.Basic, options);
            Table globals = new Table(script);

            globals.RegisterCoreModules(CoreModules.Basic);

            DynValue warn = globals.RawGet("warn");
            Assert.That(warn.IsNil(), Is.True);
        }

        [Test]
        public void RegisterCoreModulesRemovesTableMoveWhenProfileDoesNotSupportIt()
        {
            ScriptOptions options = new ScriptOptions
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua52,
            };
            Script script = new Script(CoreModules.Table, options);
            Table globals = new Table(script);

            globals.RegisterCoreModules(CoreModules.Table);

            DynValue tableNamespace = globals.RawGet("table");
            Assert.That(tableNamespace.Type, Is.EqualTo(DataType.Table));
            Assert.That(tableNamespace.Table.RawGet("move").IsNil(), Is.True);
        }

        [Test]
        public void RegisterCoreModulesThrowsWhenTableIsNull()
        {
            Assert.That(
                () => ModuleRegister.RegisterCoreModules(null, CoreModules.Basic),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("table")
            );
        }

        [Test]
        public void RegisterModuleTypeThrowsWhenArgumentsAreNull()
        {
            Table globals = new Table(new Script(CoreModules.Basic));

            Assert.That(
                () => ModuleRegister.RegisterModuleType(null, typeof(BasicModule)),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("gtable")
            );

            Assert.That(
                () => globals.RegisterModuleType((Type)null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("t")
            );
        }

        [Test]
        public void RegisterModuleTypeCreatesNamespaceAndPackageEntries()
        {
            Script script = new Script(CoreModules.Basic);
            Table globals = new Table(script);

            globals.RegisterModuleType(typeof(Bit32Module));

            DynValue namespaceValue = globals.RawGet("bit32");
            Assert.That(namespaceValue.Type, Is.EqualTo(DataType.Table));

            DynValue package = globals.RawGet("package");
            Assert.That(package.Type, Is.EqualTo(DataType.Table));

            DynValue loaded = package.Table.RawGet("loaded");
            Assert.That(loaded.Type, Is.EqualTo(DataType.Table));

            DynValue loadedNamespace = loaded.Table.RawGet("bit32");
            Assert.That(loadedNamespace.Type, Is.EqualTo(DataType.Table));
            Assert.That(loadedNamespace.Table, Is.SameAs(namespaceValue.Table));
        }
    }
}
