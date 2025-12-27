namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.CoreLib
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ModuleRegisterTUnitTests
    {
        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task RegisterCoreModulesRemovesWarnWhenProfileDoesNotSupportIt(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions { CompatibilityVersion = version };
            Script script = new Script(CoreModules.Basic, options);
            Table globals = new Table(script);

            globals.RegisterCoreModules(CoreModules.Basic);

            DynValue warn = globals.RawGet("warn");
            await Assert.That(warn.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task RegisterCoreModulesRemovesTableMoveWhenProfileDoesNotSupportIt(
            LuaCompatibilityVersion version
        )
        {
            ScriptOptions options = new ScriptOptions { CompatibilityVersion = version };
            Script script = new Script(CoreModules.Table, options);
            Table globals = new Table(script);

            globals.RegisterCoreModules(CoreModules.Table);

            DynValue tableNamespace = globals.RawGet("table");
            await Assert.That(tableNamespace.Type).IsEqualTo(DataType.Table);
            await Assert.That(tableNamespace.Table.RawGet("move").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RegisterCoreModulesThrowsWhenTableIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleRegister.RegisterCoreModules(null, CoreModules.Basic)
            );

            await Assert.That(exception.ParamName).IsEqualTo("table");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeThrowsWhenArgumentsAreNull(
            LuaCompatibilityVersion version
        )
        {
            Table globals = new Table(new Script(version, CoreModules.Basic));

            ArgumentNullException tableException = Assert.Throws<ArgumentNullException>(() =>
                ModuleRegister.RegisterModuleType(null, typeof(BasicModule))
            );
            ArgumentNullException typeException = Assert.Throws<ArgumentNullException>(() =>
                globals.RegisterModuleType((Type)null)
            );

            await Assert.That(tableException.ParamName).IsEqualTo("gtable");
            await Assert.That(typeException.ParamName).IsEqualTo("t");
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RegisterModuleTypeCreatesNamespaceAndPackageEntries(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModules.Basic);
            Table globals = new Table(script);

            globals.RegisterModuleType(typeof(Bit32Module));

            DynValue namespaceValue = globals.RawGet("bit32");
            DynValue package = globals.RawGet("package");
            DynValue loaded = package.Table.RawGet("loaded");
            DynValue loadedNamespace = loaded.Table.RawGet("bit32");

            await Assert.That(namespaceValue.Type).IsEqualTo(DataType.Table);
            await Assert.That(package.Type).IsEqualTo(DataType.Table);
            await Assert.That(loaded.Type).IsEqualTo(DataType.Table);
            await Assert.That(loadedNamespace.Type).IsEqualTo(DataType.Table);
            await Assert.That(loadedNamespace.Table).IsSameReferenceAs(namespaceValue.Table);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterConstantsThrowsWhenTableIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                ModuleRegister.RegisterConstants(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("table");
        }
    }
}
